using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MedicalRecordServiceDotnet.Domain.Entities;

/// <summary>Medical record document stored in MongoDB — linked to a patient and appointment.</summary>
public class MedicalRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [BsonElement("appointmentId")]
    public string AppointmentId { get; set; } = string.Empty;

    [BsonElement("findings")]
    public string Findings { get; set; } = string.Empty;

    [BsonElement("diagnosis")]
    public List<string> Diagnosis { get; set; } = [];

    [BsonElement("labResults")]
    public List<LabResult> LabResults { get; set; } = [];

    [BsonElement("createdBy")]
    public string? CreatedBy { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Lab result embedded document within a medical record.</summary>
public class LabResult
{
    [BsonElement("testName")]
    public string TestName { get; set; } = string.Empty;

    [BsonElement("result")]
    public string? Result { get; set; }

    [BsonElement("normalRange")]
    public string? NormalRange { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
