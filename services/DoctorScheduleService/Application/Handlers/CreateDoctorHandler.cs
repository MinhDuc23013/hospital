using DoctorScheduleService.Application.Commands;
using DoctorScheduleService.Domain.Entities;
using DoctorScheduleService.Infrastructure.HttpClients;
using DoctorScheduleService.Infrastructure.Repositories;
using HospitalShared.DTOs;
using MediatR;

namespace DoctorScheduleService.Application.Handlers;

public class CreateDoctorHandler : IRequestHandler<CreateDoctorCommand, DoctorDto>
{
    private readonly IDoctorRepository _repo;
    private readonly AuthServiceClient _authService;
    private readonly ILogger<CreateDoctorHandler> _logger;

    public CreateDoctorHandler(IDoctorRepository repo, AuthServiceClient authService, ILogger<CreateDoctorHandler> logger)
    {
        _repo = repo;
        _authService = authService;
        _logger = logger;
    }

    public async Task<DoctorDto> Handle(CreateDoctorCommand cmd, CancellationToken ct)
    {
        string? keycloakUserId = null;

        // Step 1: Create Keycloak user via Auth Service (if password provided)
        if (!string.IsNullOrWhiteSpace(cmd.Password) && !string.IsNullOrWhiteSpace(cmd.Email))
        {
            var nameParts = cmd.FullName.Split(' ', 2);
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 ? nameParts[1] : "";

            keycloakUserId = await _authService.CreateUserAsync(
                cmd.Email, cmd.Password, firstName, lastName, "doctor", ct);
            _logger.LogInformation("Keycloak user created for doctor {Email}: {KeycloakUserId}", cmd.Email, keycloakUserId);
        }

        try
        {
            // Step 2: Create Doctor record in DB
            var doctor = Doctor.Create(cmd.FullName, cmd.Specialty, cmd.Phone, cmd.Email, keycloakUserId);
            await _repo.AddAsync(doctor, ct);
            await _repo.SaveChangesAsync(ct);

            _logger.LogInformation("Doctor created: {DoctorId} KeycloakUserId={KeycloakUserId}", doctor.Id, keycloakUserId);
            return DoctorMapper.ToDto(doctor);
        }
        catch
        {
            // Compensation: delete Keycloak user if DB save fails
            if (keycloakUserId != null)
            {
                _logger.LogWarning("DB save failed, compensating Keycloak user {KeycloakUserId}", keycloakUserId);
                await _authService.DeleteUserAsync(keycloakUserId, ct);
            }
            throw;
        }
    }
}
