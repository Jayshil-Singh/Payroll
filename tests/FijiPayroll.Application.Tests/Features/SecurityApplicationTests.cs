using FijiPayroll.Application.Common.Behaviours;
using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Features.Auth.Commands.Login;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Exceptions;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FijiPayroll.Application.Tests.Features;

public interface ITestSecuredCommand : IRequest<Result<int>>, IRequirePermission
{
}

public interface ITestTransactionalCommand : IRequest<Result<int>>, ITransactional
{
}

public sealed class SecurityApplicationTests
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LoginCommandHandler> _loginLogger;

    public SecurityApplicationTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _loginLogger = Substitute.For<ILogger<LoginCommandHandler>>();
    }

    // ─── 1. Password Policy Tests ─────────────────────────────────────────────

    [Theory]
    [InlineData("Short123!")] // Too short (< 12)
    [InlineData("lowercaseonly12")] // No uppercase
    [InlineData("UPPERCASEONLY12")] // No lowercase
    [InlineData("NoNumbersHere!")] // No numbers
    [InlineData("Password1234")] // Blacklisted password
    [InlineData("Welcome12345")] // Blacklisted prefix
    public void PasswordPolicy_Validate_ShouldReturnFalse_ForInvalidPasswords(string password)
    {
        // Act
        Action act = () => PasswordPolicy.Validate(password);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("SecurePass12")]
    [InlineData("ValidPassword@123")]
    [InlineData("FijiEnterprise2026")]
    public void PasswordPolicy_Validate_ShouldReturnTrue_ForValidPasswords(string password)
    {
        // Act
        Action act = () => PasswordPolicy.Validate(password);

        // Assert
        act.Should().NotThrow();
    }

    // ─── 2. Login Flow and Lockout Tests ──────────────────────────────────────

    [Fact]
    public async Task Login_ShouldLockoutAccount_After5ConsecutiveFailures()
    {
        // Arrange
        var user = UserAccount.Create(1, "admin", "old_hash", "Admin User", mustChangePassword: true);
        
        _userRepository.GetByUsernameAsync("admin", 1, Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false); // Always fail

        var handler = new LoginCommandHandler(_userRepository, _passwordHasher, _unitOfWork, _loginLogger);
        var command = new LoginCommand("admin", "WrongPass123", 1);

        // Act & Assert
        // Attempt 1-4
        for (int i = 0; i < 4; i++)
        {
            var res = await handler.Handle(command, CancellationToken.None);
            res.IsSuccess.Should().BeFalse();
            user.IsLockedOut().Should().BeFalse();
        }

        // Attempt 5
        var finalResult = await handler.Handle(command, CancellationToken.None);
        finalResult.IsSuccess.Should().BeFalse();
        user.IsLockedOut().Should().BeTrue();
        user.FailedLoginCount.Should().Be(5);
    }

    [Fact]
    public async Task Login_SuccessfulLogin_ShouldResetFailedLoginCount()
    {
        // Arrange
        var user = UserAccount.Create(1, "admin", "hash", "Admin User", mustChangePassword: true);
        user.RecordFailedLogin(); // failed count = 1
        user.RecordFailedLogin(); // failed count = 2

        _userRepository.GetByUsernameAsync("admin", 1, Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Verify("CorrectPass123", user.PasswordHash).Returns(true);

        var handler = new LoginCommandHandler(_userRepository, _passwordHasher, _unitOfWork, _loginLogger);
        var command = new LoginCommand("admin", "CorrectPass123", 1);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.FailedLoginCount.Should().Be(0);
        user.IsLockedOut().Should().BeFalse();
    }

    // ─── 3. Authorization Behaviour Pipeline Tests ─────────────────────────────

    [Fact]
    public async Task AuthorizationBehaviour_ShouldThrowForbiddenAccessException_WhenPermissionMissing()
    {
        // Arrange
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.HasPermission("Test.Permission").Returns(false);
        var logger = Substitute.For<ILogger<AuthorizationBehaviour<ITestSecuredCommand, Result<int>>>>();

        var behavior = new AuthorizationBehaviour<ITestSecuredCommand, Result<int>>(currentUser, logger);
        var request = Substitute.For<ITestSecuredCommand>();
        request.Permission.Returns("Test.Permission");

        RequestHandlerDelegate<Result<int>> next = () => Task.FromResult(Result<int>.Success(100));

        // Act & Assert
        await behavior.Awaiting(b => b.Handle(request, next, CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task AuthorizationBehaviour_ShouldInvokeNext_WhenPermissionIsGranted()
    {
        // Arrange
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.HasPermission("Test.Permission").Returns(true);
        var logger = Substitute.For<ILogger<AuthorizationBehaviour<ITestSecuredCommand, Result<int>>>>();

        var behavior = new AuthorizationBehaviour<ITestSecuredCommand, Result<int>>(currentUser, logger);
        var request = Substitute.For<ITestSecuredCommand>();
        request.Permission.Returns("Test.Permission");

        bool nextCalled = false;
        RequestHandlerDelegate<Result<int>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Result<int>.Success(100));
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(100);
    }

    // ─── 4. Transaction Behaviour Pipeline Tests ──────────────────────────────

    [Fact]
    public async Task TransactionBehaviour_ShouldCommit_OnSuccess()
    {
        // Arrange
        var logger = Substitute.For<ILogger<TransactionBehaviour<ITestTransactionalCommand, Result<int>>>>();
        var behavior = new TransactionBehaviour<ITestTransactionalCommand, Result<int>>(_unitOfWork, logger);
        var request = Substitute.For<ITestTransactionalCommand>();

        bool nextCalled = false;
        RequestHandlerDelegate<Result<int>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Result<int>.Success(200));
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(200);

        await _unitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitTransactionAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().RollbackTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransactionBehaviour_ShouldRollback_OnFailureException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<TransactionBehaviour<ITestTransactionalCommand, Result<int>>>>();
        var behavior = new TransactionBehaviour<ITestTransactionalCommand, Result<int>>(_unitOfWork, logger);
        var request = Substitute.For<ITestTransactionalCommand>();

        RequestHandlerDelegate<Result<int>> next = () => throw new InvalidOperationException("Test calculator crash.");

        // Act & Assert
        await behavior.Awaiting(b => b.Handle(request, next, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        await _unitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitTransactionAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).RollbackTransactionAsync(Arg.Any<CancellationToken>());
    }
}
