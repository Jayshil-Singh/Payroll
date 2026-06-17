using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Features.CompanySetup.Commands.CreateCompanyWizard;
using FijiPayroll.Application.Features.CompanySetup.Queries.ValidateCompanyWizard;
using FijiPayroll.Application.Services;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FijiPayroll.Application.Tests.Features.CompanySetup;

public class CompanySetupTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISetupWorkflowService _workflowService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ValidateCompanyWizardQueryHandler> _queryLogger;
    private readonly ILogger<CreateCompanyWizardCommandHandler> _commandLogger;

    public CompanySetupTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _workflowService = Substitute.For<ISetupWorkflowService>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _queryLogger = Substitute.For<ILogger<ValidateCompanyWizardQueryHandler>>();
        _commandLogger = Substitute.For<ILogger<CreateCompanyWizardCommandHandler>>();
    }

    [Fact]
    public async Task ValidateQuery_ShouldThrowForbiddenException_WhenNoPermission()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.CompanyView).Returns(false);
        var handler = new ValidateCompanyWizardQueryHandler(_workflowService, _currentUser);
        var query = new ValidateCompanyWizardQuery(1);

        // Act
        Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task ValidateQuery_ShouldReturnFailure_WhenNoCompanyAccess()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.CompanyView).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(false);
        var handler = new ValidateCompanyWizardQueryHandler(_workflowService, _currentUser);
        var query = new ValidateCompanyWizardQuery(1);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("You do not have access to company ID 1");
    }

    [Fact]
    public async Task ValidateQuery_ShouldReturnSuccess_WhenValidationExecuted()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.CompanyView).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);
        
        var expectedDto = new ValidationResultDto(true, new List<string>(), new List<string>());
        _workflowService.ValidateSetupAsync(1, Arg.Any<CancellationToken>()).Returns(expectedDto);

        var handler = new ValidateCompanyWizardQueryHandler(_workflowService, _currentUser);
        var query = new ValidateCompanyWizardQuery(1);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedDto);
    }

    [Fact]
    public async Task CreateCommand_ShouldThrowForbiddenException_WhenNoPermission()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.CompanyEdit).Returns(false);
        var handler = new CreateCompanyWizardCommandHandler(_unitOfWork, _workflowService, _currentUser, _commandLogger);
        var command = new CreateCompanyWizardCommand(1, Guid.NewGuid());

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task CreateCommand_ShouldReturnFailure_WhenNoCompanyAccess()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.CompanyEdit).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(false);
        var handler = new CreateCompanyWizardCommandHandler(_unitOfWork, _workflowService, _currentUser, _commandLogger);
        var command = new CreateCompanyWizardCommand(1, Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("You do not have access to company ID 1");
    }

    [Fact]
    public async Task CreateCommand_ShouldReturnSuccessImmediately_WhenSetupAlreadyComplete()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.CompanyEdit).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);

        var company = Company.Create("Legal Name", "IsolatorKey");
        company.MarkSetupCompleted();
        _unitOfWork.Setup.GetCompanyByIdAsync(1, Arg.Any<CancellationToken>()).Returns(company);

        var handler = new CreateCompanyWizardCommandHandler(_unitOfWork, _workflowService, _currentUser, _commandLogger);
        var executionId = Guid.NewGuid();
        var command = new CreateCompanyWizardCommand(1, executionId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(executionId);
    }

    [Fact]
    public async Task CreateCommand_ShouldReturnSuccessImmediately_WhenCompletedExecutionRecordExists()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.CompanyEdit).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);

        var company = Company.Create("Legal Name", "IsolatorKey");
        _unitOfWork.Setup.GetCompanyByIdAsync(1, Arg.Any<CancellationToken>()).Returns(company);

        var executionId = Guid.NewGuid();
        var existingRecord = SetupExecutionRecord.Create(1, executionId, "Machine", "1.0.0");
        existingRecord.MarkCompleted();
        _unitOfWork.Setup.GetSetupExecutionRecordAsync(1, executionId, Arg.Any<CancellationToken>()).Returns(existingRecord);

        var handler = new CreateCompanyWizardCommandHandler(_unitOfWork, _workflowService, _currentUser, _commandLogger);
        var command = new CreateCompanyWizardCommand(1, executionId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(executionId);
    }

    [Fact]
    public async Task CreateCommand_ShouldReturnFailure_WhenFailedValidationCheck()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.CompanyEdit).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);

        var company = Company.Create("Legal Name", "IsolatorKey");
        _unitOfWork.Setup.GetCompanyByIdAsync(1, Arg.Any<CancellationToken>()).Returns(company);
        _unitOfWork.Setup.GetSetupExecutionRecordAsync(1, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((SetupExecutionRecord?)null);

        var validationErrors = new List<string> { "Blocking error 1" };
        var validationDto = new ValidationResultDto(false, validationErrors, new List<string>());
        _workflowService.ValidateSetupAsync(1, Arg.Any<CancellationToken>()).Returns(validationDto);

        var handler = new CreateCompanyWizardCommandHandler(_unitOfWork, _workflowService, _currentUser, _commandLogger);
        var executionId = Guid.NewGuid();
        var command = new CreateCompanyWizardCommand(1, executionId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("Blocking error 1");
    }

    [Fact]
    public async Task CreateCommand_ShouldCompleteSetup_WhenValidationSucceeds()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.CompanyEdit).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);
        _currentUser.Username.Returns("AdminUser");

        var company = Company.Create("Legal Name", "IsolatorKey");
        _unitOfWork.Setup.GetCompanyByIdAsync(1, Arg.Any<CancellationToken>()).Returns(company);
        _unitOfWork.Setup.GetSetupExecutionRecordAsync(1, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((SetupExecutionRecord?)null);

        var validationDto = new ValidationResultDto(true, new List<string>(), new List<string>());
        _workflowService.ValidateSetupAsync(1, Arg.Any<CancellationToken>()).Returns(validationDto);
        _workflowService.CompleteStepAsync(1, WizardStep.Validation, "AdminUser", Arg.Any<CancellationToken>()).Returns(Result.Success());

        var handler = new CreateCompanyWizardCommandHandler(_unitOfWork, _workflowService, _currentUser, _commandLogger);
        var executionId = Guid.NewGuid();
        var command = new CreateCompanyWizardCommand(1, executionId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(executionId);
    }
}
