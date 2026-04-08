using DoctorScheduleService.Domain.Entities;
using HospitalShared.DTOs;

namespace DoctorScheduleService.Application.Handlers;

internal static class DoctorMapper
{
    internal static DoctorDto ToDto(Doctor d) => new()
    {
        Id = d.Id,
        KeycloakUserId = d.KeycloakUserId,
        FullName = d.FullName,
        Specialty = d.Specialty,
        Phone = d.Phone,
        Email = d.Email,
        IsActive = d.IsActive,
        CreatedAt = d.CreatedAt
    };
}
