namespace HospitalShared.Auth;

/// <summary>Keycloak realm role constants — use instead of magic strings.</summary>
public static class Roles
{
    public const string Admin = "admin";
    public const string Doctor = "doctor";
    public const string Nurse = "nurse";
    public const string Pharmacist = "pharmacist";
    public const string Receptionist = "receptionist";
    public const string Patient = "patient";

    // Common combinations for [Authorize(Roles = "...")]
    public const string AdminOnly = Admin;
    public const string AdminDoctor = $"{Admin},{Doctor}";
    public const string AdminDoctorReceptionist = $"{Admin},{Doctor},{Receptionist}";
    public const string AdminDoctorReceptionistPatient = $"{Admin},{Doctor},{Receptionist},{Patient}";
    public const string AdminDoctorPharmacist = $"{Admin},{Doctor},{Pharmacist}";
    public const string AdminPharmacist = $"{Admin},{Pharmacist}";
    public const string AdminReceptionist = $"{Admin},{Receptionist}";
}
