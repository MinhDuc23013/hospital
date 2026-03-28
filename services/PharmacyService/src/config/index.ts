import dotenv from 'dotenv';
dotenv.config();

export const config = {
  port: parseInt(process.env['PORT'] ?? '5004', 10),
  database: {
    host: process.env['MSSQL_SERVER'] ?? 'localhost',
    port: parseInt(process.env['MSSQL_PORT'] ?? '1433', 10),
    username: process.env['MSSQL_USER'] ?? 'sa',
    password: process.env['MSSQL_SA_PASSWORD'] ?? 'YourStrong!Passw0rd',
    database: process.env['MSSQL_DB'] ?? 'hospital_pharmacy',
  },
  kafka: {
    brokers: (process.env['KAFKA_BROKERS'] ?? 'localhost:9092').split(','),
  },
  serviceName: 'pharmacy-service',
} as const;
