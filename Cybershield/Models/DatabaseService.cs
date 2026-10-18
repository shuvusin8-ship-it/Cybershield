using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace Cybershield.Models
{
    public sealed class AppUser
    {
        public long UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string AccountStatus { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string CyberZone { get; set; } = "Dehradun Cyber Cell";
        public int ActiveCaseCount { get; set; }
    }

    public sealed class ComplaintRecord
    {
        public string CaseId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CrimeType { get; set; } = string.Empty;
        public string ComplaintDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string OfficerRemarks { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public long? CreatedByUserId { get; set; }
        public string SuspectIdentifier { get; set; } = string.Empty;
        public decimal AmountLost { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string Priority { get; set; } = "Normal";
        public string AssignedOfficer { get; set; } = "Unassigned";
        public string CyberZone { get; set; } = "Dehradun Cyber Cell";
        public bool IsGoldenHour { get; set; }
        public string SharedWithOfficer { get; set; } = string.Empty;
        public string ShareReason { get; set; } = string.Empty;
        public string AccessRequestBy { get; set; } = string.Empty;
        public string AccessRequestType { get; set; } = string.Empty; // "ASSIGN" or "SHARE"
    }

    public sealed class EvidenceRecord
    {
        public string EvidenceId { get; set; } = string.Empty;
        public string CaseId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string StoredPath { get; set; } = string.Empty;
        public string Sha256 { get; set; } = string.Empty;
        public string AddedAt { get; set; } = string.Empty;
        public long? AddedByUserId { get; set; }
        public bool IsIntegrityVerified { get; set; }
    }

    public sealed class AuditLogRecord
    {
        public long AuditId { get; set; }
        public long? ActorUserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    public sealed class LockedAccountRecord
    {
        public string Identifier { get; set; } = string.Empty;
        public int FailureCount { get; set; }
        public string WindowStart { get; set; } = string.Empty;
        public string LockedUntil { get; set; } = string.Empty;
    }

    public sealed class AnonymousTipRecord
    {
        public long TipId { get; set; }
        public string CyberZone { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SuspectInfo { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    public sealed class SystemReport
    {
        public string ReportScope { get; set; } = "Global System Report";
        public int TotalComplaints { get; set; }
        public int TotalEvidence { get; set; }
        public int Submitted { get; set; }
        public int InProgress { get; set; }
        public int Resolved { get; set; }
        public int Closed { get; set; }
        public int Withdrawn { get; set; }
        public decimal TotalAmountLost { get; set; }
        public int HighPriorityCases { get; set; }
        public int GoldenHourCases { get; set; }
    }

    public static class AppSession
    {
        public static AppUser? CurrentUser { get; private set; }
        public static bool IsSignedIn => CurrentUser != null;

        public static string GetOfficerTag(AppUser user) => $"{user.FullName} ({user.EmployeeId})";

        public static void SignIn(AppUser user)
        {
            if (user == null || !string.Equals(user.AccountStatus, "ACTIVE", StringComparison.Ordinal))
                throw new ArgumentException("Only an active authenticated account can sign in.", nameof(user));
            CurrentUser = user;
        }

        public static void SignOut() => CurrentUser = null;

        public static bool HasRole(params string[] roles)
        {
            if (CurrentUser == null || roles == null) return false;
            foreach (string role in roles)
                if (string.Equals(CurrentUser.Role, role, StringComparison.Ordinal)) return true;
            return false;
        }

        public static void DemandRole(params string[] roles)
        {
            if (!HasRole(roles))
                throw new UnauthorizedAccessException("The current account does not have permission to perform this operation.");
        }
    }

    public static class DatabaseService
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CybercrimeManagementSystem");

        private static readonly string EvidenceFolder = Path.Combine(AppDataFolder, "EvidenceStore");
        private static readonly string DatabasePath = Path.Combine(AppDataFolder, "cybercrime.db");
        private static string ConnectionString => "Data Source=" + DatabasePath + ";Foreign Keys=True;Default Timeout=5";

        private const int PasswordIterations = 600000;
        private const int PasswordBytes = 32;
        private const int MaximumLoginFailures = 5;
        private const int LoginLockMinutes = 15;
        private const int LoginWindowMinutes = 15;

        public static readonly string[] CyberZones = new[]
        {
            "Dehradun Cyber Cell",
            "Haridwar Cyber Zone",
            "Haldwani / Nainital Cyber Cell",
            "Udham Singh Nagar Zone",
            "Garhwal Range Cyber Cell"
        };

        public static string GetDatabasePath() => DatabasePath;
        public static string GetEvidenceFolder() => EvidenceFolder;

        public static void Initialize()
        {
            Directory.CreateDirectory(AppDataFolder);
            Directory.CreateDirectory(EvidenceFolder);

            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = @"
CREATE TABLE IF NOT EXISTS Accounts (
    UserId INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName TEXT NOT NULL,
    Email TEXT NOT NULL,
    NormalizedEmail TEXT NOT NULL UNIQUE,
    Mobile TEXT NOT NULL,
    NormalizedMobile TEXT NOT NULL UNIQUE,
    PasswordSalt TEXT NOT NULL,
    PasswordHash TEXT NOT NULL,
    Role TEXT NOT NULL CHECK (Role IN ('USER', 'OFFICER', 'SUPERADMIN')),
    AccountStatus TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (AccountStatus IN ('ACTIVE', 'DISABLED')),
    EmployeeId TEXT NULL,
    NormalizedEmployeeId TEXT NULL UNIQUE,
    CyberZone TEXT NOT NULL DEFAULT 'Dehradun Cyber Cell',
    CreatedAt TEXT NOT NULL,
    CreatedByUserId INTEGER NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS UX_Accounts_OnlyOneSuperAdmin ON Accounts(Role) WHERE Role = 'SUPERADMIN';

CREATE TABLE IF NOT EXISTS OfficerInvitations (
    InvitationId INTEGER PRIMARY KEY AUTOINCREMENT,
    OfficialEmail TEXT NOT NULL,
    NormalizedEmail TEXT NOT NULL,
    EmployeeId TEXT NOT NULL,
    NormalizedEmployeeId TEXT NOT NULL,
    CyberZone TEXT NOT NULL DEFAULT 'Dehradun Cyber Cell',
    TokenHash TEXT NOT NULL UNIQUE,
    CreatedByUserId INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    ExpiresAt TEXT NOT NULL,
    UsedAt TEXT NULL
);

CREATE TABLE IF NOT EXISTS LoginAttempts (
    Identifier TEXT PRIMARY KEY,
    FailureCount INTEGER NOT NULL,
    WindowStart TEXT NOT NULL,
    LockedUntil TEXT NULL
);

CREATE TABLE IF NOT EXISTS AuditLog (
    AuditId INTEGER PRIMARY KEY AUTOINCREMENT,
    ActorUserId INTEGER NULL,
    Action TEXT NOT NULL,
    Details TEXT NOT NULL,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Complaints (
    CaseId TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    Email TEXT NOT NULL,
    CrimeType TEXT NOT NULL,
    ComplaintDate TEXT NOT NULL,
    Status TEXT NOT NULL DEFAULT 'Submitted',
    Description TEXT NOT NULL,
    OfficerRemarks TEXT NULL,
    CreatedAt TEXT NOT NULL,
    CreatedByUserId INTEGER NULL,
    SuspectIdentifier TEXT NULL,
    AmountLost REAL NOT NULL DEFAULT 0,
    TransactionId TEXT NULL,
    Priority TEXT NOT NULL DEFAULT 'Normal',
    AssignedOfficer TEXT NOT NULL DEFAULT 'Unassigned',
    CyberZone TEXT NOT NULL DEFAULT 'Dehradun Cyber Cell',
    IsGoldenHour INTEGER NOT NULL DEFAULT 0,
    SharedWithOfficer TEXT NULL,
    ShareReason TEXT NULL,
    AccessRequestBy TEXT NULL,
    AccessRequestType TEXT NULL
);

CREATE TABLE IF NOT EXISTS Evidence (
    EvidenceId TEXT PRIMARY KEY,
    CaseId TEXT NOT NULL DEFAULT 'GENERAL',
    FileName TEXT NOT NULL,
    StoredPath TEXT NOT NULL,
    Sha256 TEXT NOT NULL,
    AddedAt TEXT NOT NULL,
    AddedByUserId INTEGER NULL
);

CREATE TABLE IF NOT EXISTS AnonymousTips (
    TipId INTEGER PRIMARY KEY AUTOINCREMENT,
    CyberZone TEXT NOT NULL,
    Category TEXT NOT NULL,
    SuspectInfo TEXT NOT NULL,
    Details TEXT NOT NULL,
    CreatedAt TEXT NOT NULL
);
";
                command.ExecuteNonQuery();
            }

            // Backward-compatible migrations for existing database
            EnsureColumn("Accounts", "CyberZone", "TEXT NOT NULL DEFAULT 'Dehradun Cyber Cell'");
            EnsureColumn("OfficerInvitations", "CyberZone", "TEXT NOT NULL DEFAULT 'Dehradun Cyber Cell'");
            EnsureColumn("Complaints", "CreatedByUserId", "INTEGER NULL");
            EnsureColumn("Complaints", "OfficerRemarks", "TEXT NULL");
            EnsureColumn("Complaints", "SuspectIdentifier", "TEXT NULL");
            EnsureColumn("Complaints", "AmountLost", "REAL NOT NULL DEFAULT 0");
            EnsureColumn("Complaints", "TransactionId", "TEXT NULL");
            EnsureColumn("Complaints", "Priority", "TEXT NOT NULL DEFAULT 'Normal'");
            EnsureColumn("Complaints", "AssignedOfficer", "TEXT NOT NULL DEFAULT 'Unassigned'");
            EnsureColumn("Complaints", "CyberZone", "TEXT NOT NULL DEFAULT 'Dehradun Cyber Cell'");
            EnsureColumn("Complaints", "IsGoldenHour", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumn("Complaints", "SharedWithOfficer", "TEXT NULL");
            EnsureColumn("Complaints", "ShareReason", "TEXT NULL");
            EnsureColumn("Complaints", "AccessRequestBy", "TEXT NULL");
            EnsureColumn("Complaints", "AccessRequestType", "TEXT NULL");
            EnsureColumn("Evidence", "AddedByUserId", "INTEGER NULL");
            EnsureColumn("Evidence", "CaseId", "TEXT NOT NULL DEFAULT 'GENERAL'");
        }

        private static SqliteConnection OpenConnection()
        {
            SqliteConnection connection = new SqliteConnection(ConnectionString);
            connection.Open();
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA busy_timeout = 5000;";
                command.ExecuteNonQuery();
            }
            return connection;
        }

        private static void EnsureColumn(string tableName, string columnName, string columnDefinition)
        {
            bool found = false;
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info(" + tableName + ");";
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                            break;
                        }
                    }
                }
            }
            if (!found)
            {
                using (SqliteConnection connection = OpenConnection())
                using (SqliteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "ALTER TABLE " + tableName + " ADD COLUMN " + columnName + " " + columnDefinition + ";";
                    command.ExecuteNonQuery();
                }
            }
        }

        public static bool HasSuperAdmin()
        {
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM Accounts WHERE Role = 'SUPERADMIN';";
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
            }
        }

        public static AppUser CreateInitialSuperAdmin(string fullName, string email, string mobile, string password)
        {
            ValidateCommonRegistration(fullName, email, mobile, password, string.Empty);
            string normalizedEmail = NormalizeEmail(email);
            string normalizedMobile = NormalizeMobile(mobile);

            using (SqliteConnection connection = OpenConnection())
            using (SqliteTransaction transaction = connection.BeginTransaction())
            {
                using (SqliteCommand exists = connection.CreateCommand())
                {
                    exists.Transaction = transaction;
                    exists.CommandText = "SELECT COUNT(*) FROM Accounts WHERE Role = 'SUPERADMIN';";
                    if (Convert.ToInt32(exists.ExecuteScalar(), CultureInfo.InvariantCulture) > 0)
                        throw new InvalidOperationException("A SuperAdmin has already been initialized.");
                }

                EnsureContactUnique(connection, transaction, normalizedEmail, normalizedMobile, null);
                EnsurePasswordNotAlreadyUsed(connection, transaction, password);
                AppUser user = InsertAccount(connection, transaction, fullName.Trim(), email.Trim(), normalizedEmail,
                    normalizedMobile, mobile.Trim(), password, "SUPERADMIN", null, "State Cyber HQ", null);
                transaction.Commit();
                WriteAudit(user.UserId, "INITIAL_SUPERADMIN_CREATED", "Initial SuperAdmin account initialized.");
                return user;
            }
        }

        public static AppUser RegisterUser(string fullName, string email, string mobile, string password)
        {
            ValidateCommonRegistration(fullName, email, mobile, password, string.Empty);
            string normalizedEmail = NormalizeEmail(email);
            string normalizedMobile = NormalizeMobile(mobile);

            using (SqliteConnection connection = OpenConnection())
            using (SqliteTransaction transaction = connection.BeginTransaction())
            {
                EnsureContactUnique(connection, transaction, normalizedEmail, normalizedMobile, null);
                EnsurePasswordNotAlreadyUsed(connection, transaction, password);
                AppUser user = InsertAccount(connection, transaction, fullName.Trim(), normalizedEmail,
                    normalizedEmail, normalizedMobile, normalizedMobile, password, "USER", null, "Citizen", null);
                transaction.Commit();
                WriteAudit(user.UserId, "USER_REGISTERED", "A new user account was registered.");
                return user;
            }
        }

        public static string CreateOfficerInvitation(string officialEmail, string employeeId, string cyberZone, int expiryHours)
        {
            AppSession.DemandRole("SUPERADMIN");
            string normalizedEmail = NormalizeEmail(officialEmail);
            string normalizedEmployeeId = NormalizeEmployeeId(employeeId);
            string zone = string.IsNullOrWhiteSpace(cyberZone) ? "Dehradun Cyber Cell" : cyberZone.Trim();
            if (expiryHours < 1 || expiryHours > 168)
                throw new ArgumentException("Invitation expiry must be between 1 and 168 hours.", nameof(expiryHours));

            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand existing = connection.CreateCommand())
            {
                existing.CommandText = @"SELECT COUNT(*) FROM Accounts
WHERE NormalizedEmail = $email OR NormalizedEmployeeId = $employeeId;";
                existing.Parameters.AddWithValue("$email", normalizedEmail);
                existing.Parameters.AddWithValue("$employeeId", normalizedEmployeeId);
                if (Convert.ToInt32(existing.ExecuteScalar(), CultureInfo.InvariantCulture) > 0)
                    throw new InvalidOperationException("An account already exists with this email or Employee ID.");
            }

            string token = "OFF-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            string tokenHash = Sha256Text(token);
            DateTime now = DateTime.UtcNow;
            DateTime expiry = now.AddHours(expiryHours);

            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = @"INSERT INTO OfficerInvitations
(OfficialEmail, NormalizedEmail, EmployeeId, NormalizedEmployeeId, CyberZone, TokenHash, CreatedByUserId, CreatedAt, ExpiresAt, UsedAt)
VALUES ($email, $normalizedEmail, $employeeId, $normalizedEmployeeId, $zone, $tokenHash, $createdBy, $createdAt, $expiresAt, NULL);";
                command.Parameters.AddWithValue("$email", normalizedEmail);
                command.Parameters.AddWithValue("$normalizedEmail", normalizedEmail);
                command.Parameters.AddWithValue("$employeeId", employeeId.Trim());
                command.Parameters.AddWithValue("$normalizedEmployeeId", normalizedEmployeeId);
                command.Parameters.AddWithValue("$zone", zone);
                command.Parameters.AddWithValue("$tokenHash", tokenHash);
                command.Parameters.AddWithValue("$createdBy", AppSession.CurrentUser!.UserId);
                command.Parameters.AddWithValue("$createdAt", UtcText(now));
                command.Parameters.AddWithValue("$expiresAt", UtcText(expiry));
                command.ExecuteNonQuery();
            }

            WriteAudit(AppSession.CurrentUser.UserId, "OFFICER_INVITATION_CREATED",
                $"Officer invitation created for {normalizedEmployeeId} ({zone}).");
            return token;
        }

        public static AppUser RegisterOfficer(string fullName, string email, string mobile,
            string employeeId, string invitationToken, string password)
        {
            ValidateCommonRegistration(fullName, email, mobile, password, employeeId);
            if (string.IsNullOrWhiteSpace(invitationToken))
                throw new ArgumentException("An invitation code is required.", nameof(invitationToken));

            string normalizedEmail = NormalizeEmail(email);
            string normalizedMobile = NormalizeMobile(mobile);
            string normalizedEmployeeId = NormalizeEmployeeId(employeeId);
            string tokenHash = Sha256Text(invitationToken.Trim());
            DateTime now = DateTime.UtcNow;

            using (SqliteConnection connection = OpenConnection())
            using (SqliteTransaction transaction = connection.BeginTransaction())
            {
                long invitationId;
                long createdByUserId;
                string assignedZone = "Dehradun Cyber Cell";
                using (SqliteCommand invite = connection.CreateCommand())
                {
                    invite.Transaction = transaction;
                    invite.CommandText = @"SELECT InvitationId, CreatedByUserId, COALESCE(CyberZone, 'Dehradun Cyber Cell') FROM OfficerInvitations
WHERE NormalizedEmail = $email AND NormalizedEmployeeId = $employeeId
AND TokenHash = $tokenHash AND UsedAt IS NULL AND ExpiresAt > $now LIMIT 1;";
                    invite.Parameters.AddWithValue("$email", normalizedEmail);
                    invite.Parameters.AddWithValue("$employeeId", normalizedEmployeeId);
                    invite.Parameters.AddWithValue("$tokenHash", tokenHash);
                    invite.Parameters.AddWithValue("$now", UtcText(now));
                    using (SqliteDataReader reader = invite.ExecuteReader())
                    {
                        if (!reader.Read())
                            throw new InvalidOperationException("The invitation code is invalid, expired, already used, or does not match.");
                        invitationId = reader.GetInt64(0);
                        createdByUserId = reader.GetInt64(1);
                        assignedZone = reader.GetString(2);
                    }
                }

                EnsureContactUnique(connection, transaction, normalizedEmail, normalizedMobile, normalizedEmployeeId);
                EnsurePasswordNotAlreadyUsed(connection, transaction, password);
                AppUser user = InsertAccount(connection, transaction, fullName.Trim(), normalizedEmail,
                    normalizedEmail, normalizedMobile, normalizedMobile, password, "OFFICER",
                    normalizedEmployeeId, assignedZone, createdByUserId);

                using (SqliteCommand consume = connection.CreateCommand())
                {
                    consume.Transaction = transaction;
                    consume.CommandText = "UPDATE OfficerInvitations SET UsedAt = $usedAt WHERE InvitationId = $id AND UsedAt IS NULL;";
                    consume.Parameters.AddWithValue("$usedAt", UtcText(now));
                    consume.Parameters.AddWithValue("$id", invitationId);
                    consume.ExecuteNonQuery();
                }

                transaction.Commit();
                WriteAudit(user.UserId, "OFFICER_REGISTERED", $"Officer {normalizedEmployeeId} activated in {assignedZone}.");
                return user;
            }
        }

        public static AppUser? Authenticate(string emailOrMobile, string password)
        {
            if (string.IsNullOrWhiteSpace(emailOrMobile) || string.IsNullOrEmpty(password)) return null;
            string identifier = NormalizeLoginIdentifier(emailOrMobile);
            if (identifier.Length == 0 || GetLockoutRemainingSeconds(identifier) > 0) return null;

            AppUser? user = null;
            string? salt = null;
            string? storedHash = null;

            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                string col = identifier.Contains("@") ? "NormalizedEmail" : "NormalizedMobile";
                command.CommandText = $@"SELECT UserId, FullName, Email, Mobile, Role, AccountStatus, EmployeeId, PasswordSalt, PasswordHash, COALESCE(CyberZone, 'Dehradun Cyber Cell')
FROM Accounts WHERE {col} = $identifier LIMIT 1;";
                command.Parameters.AddWithValue("$identifier", identifier);

                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        user = new AppUser
                        {
                            UserId = reader.GetInt64(0),
                            FullName = reader.GetString(1),
                            Email = reader.GetString(2),
                            Mobile = reader.GetString(3),
                            Role = reader.GetString(4),
                            AccountStatus = reader.GetString(5),
                            EmployeeId = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                            CyberZone = reader.GetString(9)
                        };
                        salt = reader.GetString(7);
                        storedHash = reader.GetString(8);
                    }
                }
            }

            if (user == null || !string.Equals(user.AccountStatus, "ACTIVE", StringComparison.Ordinal) ||
                !VerifyPassword(password, salt!, storedHash!))
            {
                RecordFailedLogin(identifier);
                return null;
            }

            ClearLoginFailures(identifier);
            WriteAudit(user.UserId, "LOGIN_SUCCESS", "Successful login for role " + user.Role + ".");
            return user;
        }

        public static int GetLockoutRemainingSeconds(string emailOrMobile)
        {
            string identifier = NormalizeLoginIdentifier(emailOrMobile);
            if (identifier.Length == 0) return 0;

            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT LockedUntil FROM LoginAttempts WHERE Identifier = $identifier LIMIT 1;";
                command.Parameters.AddWithValue("$identifier", identifier);
                object? value = command.ExecuteScalar();
                if (value == null || value == DBNull.Value) return 0;
                if (!DateTime.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime lockedUntil))
                    return 0;
                return Math.Max(0, (int)Math.Ceiling((lockedUntil - DateTime.UtcNow).TotalSeconds));
            }
        }

        private static void RecordFailedLogin(string identifier)
        {
            DateTime now = DateTime.UtcNow;
            using (SqliteConnection connection = OpenConnection())
            using (SqliteTransaction transaction = connection.BeginTransaction())
            {
                int failures = 0;
                DateTime windowStart = now;
                using (SqliteCommand read = connection.CreateCommand())
                {
                    read.Transaction = transaction;
                    read.CommandText = "SELECT FailureCount, WindowStart FROM LoginAttempts WHERE Identifier = $id LIMIT 1;";
                    read.Parameters.AddWithValue("$id", identifier);
                    using (SqliteDataReader reader = read.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            failures = reader.GetInt32(0);
                            DateTime.TryParse(reader.GetString(1), CultureInfo.InvariantCulture,
                                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out windowStart);
                        }
                    }
                }

                if (windowStart < now.AddMinutes(-LoginWindowMinutes)) { failures = 1; windowStart = now; }
                else { failures++; }

                DateTime? newLock = failures >= MaximumLoginFailures ? now.AddMinutes(LoginLockMinutes) : (DateTime?)null;
                using (SqliteCommand save = connection.CreateCommand())
                {
                    save.Transaction = transaction;
                    save.CommandText = @"INSERT INTO LoginAttempts (Identifier, FailureCount, WindowStart, LockedUntil)
VALUES ($id, $failures, $windowStart, $lockedUntil)
ON CONFLICT(Identifier) DO UPDATE SET FailureCount = excluded.FailureCount, WindowStart = excluded.WindowStart, LockedUntil = excluded.LockedUntil;";
                    save.Parameters.AddWithValue("$id", identifier);
                    save.Parameters.AddWithValue("$failures", failures);
                    save.Parameters.AddWithValue("$windowStart", UtcText(windowStart));
                    save.Parameters.AddWithValue("$lockedUntil", newLock.HasValue ? UtcText(newLock.Value) : (object)DBNull.Value);
                    save.ExecuteNonQuery();
                }
                transaction.Commit();
            }
        }

        private static void ClearLoginFailures(string identifier)
        {
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM LoginAttempts WHERE Identifier = $id;";
                command.Parameters.AddWithValue("$id", identifier);
                command.ExecuteNonQuery();
            }
        }

        public static bool SetAccountStatus(long targetUserId, string newStatus)
        {
            AppSession.DemandRole("SUPERADMIN");
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE Accounts SET AccountStatus = $status WHERE UserId = $id AND Role <> 'SUPERADMIN';";
                command.Parameters.AddWithValue("$status", newStatus);
                command.Parameters.AddWithValue("$id", targetUserId);
                bool changed = command.ExecuteNonQuery() > 0;
                if (changed) WriteAudit(AppSession.CurrentUser!.UserId, "ACCOUNT_STATUS_CHANGED", $"Account {targetUserId} set to {newStatus}.");
                return changed;
            }
        }

        private static AppUser InsertAccount(SqliteConnection connection, SqliteTransaction transaction,
            string fullName, string email, string normalizedEmail, string normalizedMobile,
            string displayMobile, string password, string role, string? employeeId, string cyberZone, long? createdByUserId)
        {
            string salt = CreateSalt();
            string hash = HashPassword(password, salt);
            string createdAt = UtcText(DateTime.UtcNow);
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"INSERT INTO Accounts
(FullName, Email, NormalizedEmail, Mobile, NormalizedMobile, PasswordSalt, PasswordHash, Role, AccountStatus, EmployeeId, NormalizedEmployeeId, CyberZone, CreatedAt, CreatedByUserId)
VALUES ($fullName, $email, $normalizedEmail, $mobile, $normalizedMobile, $salt, $hash, $role, 'ACTIVE', $employeeId, $normalizedEmployeeId, $zone, $createdAt, $createdBy);";
                command.Parameters.AddWithValue("$fullName", fullName);
                command.Parameters.AddWithValue("$email", email);
                command.Parameters.AddWithValue("$normalizedEmail", normalizedEmail);
                command.Parameters.AddWithValue("$mobile", displayMobile);
                command.Parameters.AddWithValue("$normalizedMobile", normalizedMobile);
                command.Parameters.AddWithValue("$salt", salt);
                command.Parameters.AddWithValue("$hash", hash);
                command.Parameters.AddWithValue("$role", role);
                command.Parameters.AddWithValue("$employeeId", string.IsNullOrEmpty(employeeId) ? (object)DBNull.Value : employeeId);
                command.Parameters.AddWithValue("$normalizedEmployeeId", string.IsNullOrEmpty(employeeId) ? (object)DBNull.Value : NormalizeEmployeeId(employeeId));
                command.Parameters.AddWithValue("$zone", cyberZone);
                command.Parameters.AddWithValue("$createdAt", createdAt);
                command.Parameters.AddWithValue("$createdBy", createdByUserId.HasValue ? (object)createdByUserId.Value : DBNull.Value);
                command.ExecuteNonQuery();
            }

            long id;
            using (SqliteCommand idCommand = connection.CreateCommand())
            {
                idCommand.Transaction = transaction;
                idCommand.CommandText = "SELECT last_insert_rowid();";
                id = (long)idCommand.ExecuteScalar()!;
            }

            return new AppUser
            {
                UserId = id,
                FullName = fullName,
                Email = email,
                Mobile = displayMobile,
                Role = role,
                AccountStatus = "ACTIVE",
                EmployeeId = employeeId ?? string.Empty,
                CyberZone = cyberZone
            };
        }

        private static void EnsurePasswordNotAlreadyUsed(SqliteConnection connection, SqliteTransaction transaction, string candidatePassword)
        {
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "SELECT PasswordSalt, PasswordHash FROM Accounts;";
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        if (VerifyPassword(candidatePassword, reader.GetString(0), reader.GetString(1)))
                            throw new InvalidOperationException("This password is already used by another account. Choose a different password.");
                }
            }
        }

        private static void EnsureContactUnique(SqliteConnection connection, SqliteTransaction transaction,
            string normalizedEmail, string normalizedMobile, string? normalizedEmployeeId)
        {
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"SELECT
EXISTS(SELECT 1 FROM Accounts WHERE NormalizedEmail = $email),
EXISTS(SELECT 1 FROM Accounts WHERE NormalizedMobile = $mobile),
CASE WHEN $employeeId IS NULL THEN 0 ELSE EXISTS(SELECT 1 FROM Accounts WHERE NormalizedEmployeeId = $employeeId) END;";
                command.Parameters.AddWithValue("$email", normalizedEmail);
                command.Parameters.AddWithValue("$mobile", normalizedMobile);
                command.Parameters.AddWithValue("$employeeId", string.IsNullOrEmpty(normalizedEmployeeId) ? (object)DBNull.Value : normalizedEmployeeId);
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    reader.Read();
                    if (reader.GetInt32(0) > 0) throw new InvalidOperationException("This email address is already registered.");
                    if (reader.GetInt32(1) > 0) throw new InvalidOperationException("This mobile number is already registered.");
                    if (reader.GetInt32(2) > 0) throw new InvalidOperationException("This Employee ID is already registered.");
                }
            }
        }

        private static void ValidateCommonRegistration(string fullName, string email, string mobile, string password, string employeeId)
        {
            if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length < 2 || fullName.Trim().Length > 100)
                throw new ArgumentException("Enter a name containing 2 to 100 characters.");
            string normalizedEmail = NormalizeEmail(email);
            string normalizedMobile = NormalizeMobile(mobile);
            if (string.IsNullOrWhiteSpace(password) || password.Length < 10 || password.Length > 128)
                throw new ArgumentException("Password must be 10 to 128 characters long.");

            bool upper = false, lower = false, digit = false, special = false;
            foreach (char ch in password)
            {
                upper |= char.IsUpper(ch);
                lower |= char.IsLower(ch);
                digit |= char.IsDigit(ch);
                special |= !char.IsLetterOrDigit(ch);
            }
            if (!(upper && lower && digit && special))
                throw new ArgumentException("Password must include an uppercase letter, a lowercase letter, a number, and a special character.");
            if (!string.IsNullOrWhiteSpace(employeeId)) NormalizeEmployeeId(employeeId);
        }

        private static string NormalizeEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email address is required.");
            MailAddress parsed = new MailAddress(email.Trim());
            return parsed.Address.ToLowerInvariant();
        }

        private static string NormalizeMobile(string mobile)
        {
            string digits = Regex.Replace(mobile ?? "", "[^0-9]", string.Empty);
            if (digits.Length == 12 && digits.StartsWith("91", StringComparison.Ordinal)) digits = digits.Substring(2);
            if (digits.Length < 10 || digits.Length > 15) throw new ArgumentException("Enter a valid 10 to 15 digit mobile number.");
            return digits;
        }

        private static string NormalizeEmployeeId(string employeeId)
        {
            string normalized = Regex.Replace((employeeId ?? "").Trim().ToUpperInvariant(), "[^A-Z0-9-]", string.Empty);
            if (normalized.Length < 3 || normalized.Length > 40) throw new ArgumentException("Employee ID must be 3 to 40 letters, digits, or hyphens.");
            return normalized;
        }

        private static string NormalizeLoginIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            string trimmed = value.Trim();
            if (trimmed.Contains("@")) return trimmed.ToLowerInvariant();
            string digits = Regex.Replace(trimmed, "[^0-9]", string.Empty);
            if (digits.Length == 12 && digits.StartsWith("91", StringComparison.Ordinal)) digits = digits.Substring(2);
            return (digits.Length >= 10 && digits.Length <= 15) ? digits : string.Empty;
        }

        private static string CreateSalt() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        private static string HashPassword(string password, string saltHex) =>
            Convert.ToHexString(Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromHexString(saltHex), PasswordIterations, HashAlgorithmName.SHA256, PasswordBytes));

        private static bool VerifyPassword(string password, string saltHex, string storedHashHex) =>
            CryptographicOperations.FixedTimeEquals(Convert.FromHexString(HashPassword(password, saltHex)), Convert.FromHexString(storedHashHex));

        private static string Sha256Text(string value)
        {
            using (SHA256 sha = SHA256.Create()) return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(value)));
        }

        private static string UtcText(DateTime value) => value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

        private static void WriteAudit(long? actorUserId, string action, string details)
        {
            try
            {
                using (SqliteConnection connection = OpenConnection())
                using (SqliteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO AuditLog (ActorUserId, Action, Details, CreatedAt) VALUES ($actor, $action, $details, $createdAt);";
                    command.Parameters.AddWithValue("$actor", actorUserId.HasValue ? (object)actorUserId.Value : DBNull.Value);
                    command.Parameters.AddWithValue("$action", action);
                    command.Parameters.AddWithValue("$details", details);
                    command.Parameters.AddWithValue("$createdAt", UtcText(DateTime.UtcNow));
                    command.ExecuteNonQuery();
                }
            }
            catch { }
        }

        // ==================== COMPLAINTS & MASTER WORKFLOW ====================

        private const string ComplaintSelectColumns = @"CaseId, Name, Email, CrimeType, ComplaintDate, Status, Description, CreatedAt, CreatedByUserId,
OfficerRemarks, SuspectIdentifier, AmountLost, TransactionId, Priority, AssignedOfficer, CyberZone, IsGoldenHour, SharedWithOfficer, ShareReason, AccessRequestBy, AccessRequestType";

        public static string CreateComplaint(string name, string email, string crimeType,
            DateTime complaintDate, string description, long createdByUserId,
            string? suspectIdentifier, decimal amountLost, string? transactionId, string? cyberZone)
        {
            AppSession.DemandRole("USER");
            string zone = string.IsNullOrWhiteSpace(cyberZone) ? "Dehradun Cyber Cell" : cyberZone.Trim();
            bool isGoldenHour = (amountLost >= 10000 && (DateTime.Today - complaintDate.Date).TotalDays <= 1);
            string priority = (isGoldenHour || amountLost >= 10000 || crimeType == "Online Fraud" || crimeType == "Hacking") ? "High" : "Normal";

            string caseId = "CASE" + DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) +
                            Guid.NewGuid().ToString("N").Substring(0, 4).ToUpperInvariant();
            string createdAt = UtcText(DateTime.UtcNow);

            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = @"INSERT INTO Complaints
(CaseId, Name, Email, CrimeType, ComplaintDate, Status, Description, CreatedAt, CreatedByUserId, SuspectIdentifier, AmountLost, TransactionId, Priority, AssignedOfficer, CyberZone, IsGoldenHour)
VALUES ($caseId, $name, $email, $crimeType, $complaintDate, 'Submitted', $description, $createdAt, $userId, $suspect, $amount, $txnId, $priority, 'Unassigned', $zone, $golden);";
                command.Parameters.AddWithValue("$caseId", caseId);
                command.Parameters.AddWithValue("$name", name.Trim());
                command.Parameters.AddWithValue("$email", email.Trim().ToLowerInvariant());
                command.Parameters.AddWithValue("$crimeType", crimeType.Trim());
                command.Parameters.AddWithValue("$complaintDate", complaintDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$description", description.Trim());
                command.Parameters.AddWithValue("$createdAt", createdAt);
                command.Parameters.AddWithValue("$userId", createdByUserId);
                command.Parameters.AddWithValue("$suspect", string.IsNullOrWhiteSpace(suspectIdentifier) ? (object)DBNull.Value : suspectIdentifier.Trim().ToLowerInvariant());
                command.Parameters.AddWithValue("$amount", amountLost);
                command.Parameters.AddWithValue("$txnId", string.IsNullOrWhiteSpace(transactionId) ? (object)DBNull.Value : transactionId.Trim());
                command.Parameters.AddWithValue("$priority", priority);
                command.Parameters.AddWithValue("$zone", zone);
                command.Parameters.AddWithValue("$golden", isGoldenHour ? 1 : 0);
                command.ExecuteNonQuery();
            }
            WriteAudit(createdByUserId, "COMPLAINT_CREATED", $"Case {caseId} filed under {zone} (GoldenHour: {isGoldenHour}).");
            return caseId;
        }

        private static ComplaintRecord ReadComplaint(SqliteDataReader reader)
        {
            return new ComplaintRecord
            {
                CaseId = reader.GetString(0),
                Name = reader.GetString(1),
                Email = reader.GetString(2),
                CrimeType = reader.GetString(3),
                ComplaintDate = reader.GetString(4),
                Status = reader.GetString(5),
                Description = reader.GetString(6),
                CreatedAt = reader.GetString(7),
                CreatedByUserId = reader.IsDBNull(8) ? (long?)null : reader.GetInt64(8),
                OfficerRemarks = !reader.IsDBNull(9) ? reader.GetString(9) : string.Empty,
                SuspectIdentifier = !reader.IsDBNull(10) ? reader.GetString(10) : string.Empty,
                AmountLost = !reader.IsDBNull(11) ? reader.GetDecimal(11) : 0,
                TransactionId = !reader.IsDBNull(12) ? reader.GetString(12) : string.Empty,
                Priority = !reader.IsDBNull(13) ? reader.GetString(13) : "Normal",
                AssignedOfficer = !reader.IsDBNull(14) ? reader.GetString(14) : "Unassigned",
                CyberZone = !reader.IsDBNull(15) ? reader.GetString(15) : "Dehradun Cyber Cell",
                IsGoldenHour = !reader.IsDBNull(16) && reader.GetInt32(16) == 1,
                SharedWithOfficer = !reader.IsDBNull(17) ? reader.GetString(17) : string.Empty,
                ShareReason = !reader.IsDBNull(18) ? reader.GetString(18) : string.Empty,
                AccessRequestBy = !reader.IsDBNull(19) ? reader.GetString(19) : string.Empty,
                AccessRequestType = !reader.IsDBNull(20) ? reader.GetString(20) : string.Empty
            };
        }

        // Strict Access Check (Q2: Officer can only view if Assigned or Shared)
        public static ComplaintRecord? GetComplaintByCaseId(string caseId, AppUser requester)
        {
            if (requester == null || string.IsNullOrWhiteSpace(caseId)) return null;
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                if (requester.Role == "USER")
                {
                    command.CommandText = "SELECT " + ComplaintSelectColumns + " FROM Complaints WHERE CaseId = $caseId AND CreatedByUserId = $userId LIMIT 1;";
                    command.Parameters.AddWithValue("$userId", requester.UserId);
                }
                else if (requester.Role == "OFFICER")
                {
                    string myTag = AppSession.GetOfficerTag(requester);
                    command.CommandText = "SELECT " + ComplaintSelectColumns + " FROM Complaints WHERE CaseId = $caseId AND (AssignedOfficer = $tag OR SharedWithOfficer = $tag) LIMIT 1;";
                    command.Parameters.AddWithValue("$tag", myTag);
                }
                else // SUPERADMIN
                {
                    command.CommandText = "SELECT " + ComplaintSelectColumns + " FROM Complaints WHERE CaseId = $caseId LIMIT 1;";
                }
                command.Parameters.AddWithValue("$caseId", caseId.Trim());
                using (SqliteDataReader reader = command.ExecuteReader())
                    return reader.Read() ? ReadComplaint(reader) : null;
            }
        }

        // Strict List: SuperAdmin sees all, Officer sees ONLY Assigned or Shared cases
        public static List<ComplaintRecord> GetAllComplaints()
        {
            AppSession.DemandRole("OFFICER", "SUPERADMIN");
            List<ComplaintRecord> records = new List<ComplaintRecord>();
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                if (AppSession.CurrentUser!.Role == "SUPERADMIN")
                {
                    command.CommandText = "SELECT " + ComplaintSelectColumns + " FROM Complaints ORDER BY IsGoldenHour DESC, CreatedAt DESC;";
                }
                else
                {
                    string myTag = AppSession.GetOfficerTag(AppSession.CurrentUser);
                    command.CommandText = "SELECT " + ComplaintSelectColumns + " FROM Complaints WHERE AssignedOfficer = $tag OR SharedWithOfficer = $tag ORDER BY IsGoldenHour DESC, CreatedAt DESC;";
                    command.Parameters.AddWithValue("$tag", myTag);
                }
                using (SqliteDataReader reader = command.ExecuteReader())
                    while (reader.Read()) records.Add(ReadComplaint(reader));
            }
            return records;
        }

        // Incoming Unassigned Cases in Officer's Jurisdiction (Q4 Notification Queue)
        public static List<ComplaintRecord> GetIncomingZoneCasesForOfficer(AppUser officer)
        {
            List<ComplaintRecord> list = new List<ComplaintRecord>();
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT " + ComplaintSelectColumns + " FROM Complaints WHERE AssignedOfficer = 'Unassigned' AND CyberZone = $zone ORDER BY IsGoldenHour DESC, CreatedAt DESC;";
                command.Parameters.AddWithValue("$zone", officer.CyberZone);
                using (SqliteDataReader reader = command.ExecuteReader())
                    while (reader.Read()) list.Add(ReadComplaint(reader));
            }
            return list;
        }

        // Officer Requests Access to an Unassigned Case in their Zone
        public static bool RequestCaseAccess(string caseId)
        {
            AppSession.DemandRole("OFFICER");
            string myTag = AppSession.GetOfficerTag(AppSession.CurrentUser!);
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = @"UPDATE Complaints SET AccessRequestBy = $tag, AccessRequestType = 'ASSIGN'
WHERE CaseId = $caseId AND AssignedOfficer = 'Unassigned';";
                command.Parameters.AddWithValue("$tag", myTag);
                command.Parameters.AddWithValue("$caseId", caseId.Trim());
                bool ok = command.ExecuteNonQuery() > 0;
                if (ok) WriteAudit(AppSession.CurrentUser!.UserId, "CASE_ACCESS_REQUESTED", $"{myTag} requested investigation access for {caseId}.");
                return ok;
            }
        }

        // Officer Requests to Share Case with another Officer (Anti-Misuse: Mandatory Reason + SuperAdmin Approval)
        public static bool RequestCaseShare(string caseId, string targetOfficerTag, string reason)
        {
            AppSession.DemandRole("OFFICER");
            if (string.IsNullOrWhiteSpace(targetOfficerTag) || string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Target officer and mandatory reason are required to share a case.");

            string myTag = AppSession.GetOfficerTag(AppSession.CurrentUser!);
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = @"UPDATE Complaints SET AccessRequestBy = $target, AccessRequestType = 'SHARE', ShareReason = $reason
WHERE CaseId = $caseId AND AssignedOfficer = $myTag;";
                command.Parameters.AddWithValue("$target", targetOfficerTag.Trim());
                command.Parameters.AddWithValue("$reason", $"[Shared by {myTag}]: {reason.Trim()}");
                command.Parameters.AddWithValue("$caseId", caseId.Trim());
                command.Parameters.AddWithValue("$myTag", myTag);
                bool ok = command.ExecuteNonQuery() > 0;
                if (ok) WriteAudit(AppSession.CurrentUser!.UserId, "CASE_SHARE_REQUESTED", $"{myTag} requested to share {caseId} with {targetOfficerTag}. Reason: {reason}");
                return ok;
            }
        }

        // SuperAdmin Approves or Rejects Officer Access / Share Request
        public static bool ProcessAccessRequest(string caseId, bool approve)
        {
            AppSession.DemandRole("SUPERADMIN");
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                if (approve)
                {
                    command.CommandText = @"UPDATE Complaints SET
AssignedOfficer = CASE WHEN AccessRequestType = 'ASSIGN' THEN AccessRequestBy ELSE AssignedOfficer END,
SharedWithOfficer = CASE WHEN AccessRequestType = 'SHARE' THEN AccessRequestBy ELSE SharedWithOfficer END,
AccessRequestBy = NULL, AccessRequestType = NULL
WHERE CaseId = $caseId AND AccessRequestBy IS NOT NULL;";
                }
                else
                {
                    command.CommandText = "UPDATE Complaints SET AccessRequestBy = NULL, AccessRequestType = NULL WHERE CaseId = $caseId;";
                }
                command.Parameters.AddWithValue("$caseId", caseId.Trim());
                bool ok = command.ExecuteNonQuery() > 0;
                if (ok) WriteAudit(AppSession.CurrentUser!.UserId, approve ? "ACCESS_REQUEST_APPROVED" : "ACCESS_REQUEST_REJECTED", $"Processed request for {caseId}.");
                return ok;
            }
        }

        // SuperAdmin Direct Assign / Emergency Re-assign (Automatically unassigns from previous officer)
        public static bool AssignCaseToOfficer(string caseId, string officerInfo)
        {
            AppSession.DemandRole("SUPERADMIN");
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE Complaints SET AssignedOfficer = $officer, AccessRequestBy = NULL, AccessRequestType = NULL WHERE CaseId = $caseId;";
                command.Parameters.AddWithValue("$officer", string.IsNullOrWhiteSpace(officerInfo) ? "Unassigned" : officerInfo.Trim());
                command.Parameters.AddWithValue("$caseId", caseId.Trim());
                bool ok = command.ExecuteNonQuery() > 0;
                if (ok) WriteAudit(AppSession.CurrentUser!.UserId, "CASE_ASSIGNED_OR_REASSIGNED", $"Case {caseId} assigned to {officerInfo} (unassigned from previous).");
                return ok;
            }
        }

        // Q1: ONLY Primary Assigned Officer can change status; remarks append to history so input box clears!
        public static bool UpdateComplaintStatus(string caseId, string status, string? newRemark)
        {
            AppSession.DemandRole("OFFICER");
            string myTag = AppSession.GetOfficerTag(AppSession.CurrentUser!);
            string stampedRemark = string.IsNullOrWhiteSpace(newRemark)
                ? ""
                : $"[{DateTime.Now:dd-MMM-yyyy HH:mm} - {myTag}]: {newRemark.Trim()}\n";

            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = @"UPDATE Complaints SET Status = $status,
OfficerRemarks = CASE WHEN $rem = '' THEN OfficerRemarks ELSE COALESCE(OfficerRemarks, '') || $rem END
WHERE CaseId = $caseId AND AssignedOfficer = $myTag;";
                command.Parameters.AddWithValue("$status", status);
                command.Parameters.AddWithValue("$rem", stampedRemark);
                command.Parameters.AddWithValue("$caseId", caseId.Trim());
                command.Parameters.AddWithValue("$myTag", myTag);
                bool updated = command.ExecuteNonQuery() > 0;
                if (updated) WriteAudit(AppSession.CurrentUser!.UserId, "CASE_STATUS_UPDATED", $"Case {caseId} updated to {status} by {myTag}.");
                return updated;
            }
        }

        public static List<ComplaintRecord> GetComplaintsByUserId(long userId)
        {
            List<ComplaintRecord> records = new List<ComplaintRecord>();
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT " + ComplaintSelectColumns + " FROM Complaints WHERE CreatedByUserId = $userId ORDER BY CreatedAt DESC;";
                command.Parameters.AddWithValue("$userId", userId);
                using (SqliteDataReader reader = command.ExecuteReader())
                    while (reader.Read()) records.Add(ReadComplaint(reader));
            }
            return records;
        }

        public static bool WithdrawComplaint(string caseId, long userId)
        {
            AppSession.DemandRole("USER");
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = @"UPDATE Complaints SET Status = 'Withdrawn',
OfficerRemarks = COALESCE(OfficerRemarks, '') || '[Withdrawn by Complainant]\n'
WHERE CaseId = $caseId AND CreatedByUserId = $userId AND Status = 'Submitted';";
                command.Parameters.AddWithValue("$caseId", caseId.Trim());
                command.Parameters.AddWithValue("$userId", userId);
                return command.ExecuteNonQuery() > 0;
            }
        }

        public static bool AddComplaintFollowUp(string caseId, long userId, string additionalNote)
        {
            AppSession.DemandRole("USER");
            if (string.IsNullOrWhiteSpace(additionalNote)) return false;
            string stampedNote = $"\n[Citizen Follow-Up {DateTime.Now:dd-MMM HH:mm}]: {additionalNote.Trim()}";
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = @"UPDATE Complaints SET Description = Description || $note
WHERE CaseId = $caseId AND CreatedByUserId = $userId AND Status NOT IN ('Closed', 'Withdrawn');";
                command.Parameters.AddWithValue("$note", stampedNote);
                command.Parameters.AddWithValue("$caseId", caseId.Trim());
                command.Parameters.AddWithValue("$userId", userId);
                return command.ExecuteNonQuery() > 0;
            }
        }

        // ==================== EVIDENCE & LIVE SHA-256 TAMPER CHECK ====================

        public static EvidenceRecord AddEvidenceFile(string caseId, string sourcePath)
        {
            if (!AppSession.IsSignedIn) throw new UnauthorizedAccessException("Sign in required.");
            string evidenceId = "EVD" + DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) +
                                Guid.NewGuid().ToString("N").Substring(0, 4).ToUpperInvariant();
            string fileName = Path.GetFileName(sourcePath);
            string storedPath = Path.Combine(EvidenceFolder, evidenceId + "_" + fileName);

            File.Copy(sourcePath, storedPath, false);
            string hash = ComputeSha256(storedPath);
            string addedAt = UtcText(DateTime.UtcNow);
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = @"INSERT INTO Evidence (EvidenceId, CaseId, FileName, StoredPath, Sha256, AddedAt, AddedByUserId)
VALUES ($id, $caseId, $name, $path, $hash, $addedAt, $userId);";
                command.Parameters.AddWithValue("$id", evidenceId);
                command.Parameters.AddWithValue("$caseId", caseId.Trim());
                command.Parameters.AddWithValue("$name", fileName);
                command.Parameters.AddWithValue("$path", storedPath);
                command.Parameters.AddWithValue("$hash", hash);
                command.Parameters.AddWithValue("$addedAt", addedAt);
                command.Parameters.AddWithValue("$userId", AppSession.CurrentUser!.UserId);
                command.ExecuteNonQuery();
            }
            WriteAudit(AppSession.CurrentUser.UserId, "EVIDENCE_ADDED", $"Evidence {evidenceId} linked to Case {caseId}.");
            return new EvidenceRecord { EvidenceId = evidenceId, CaseId = caseId.Trim(), FileName = fileName, StoredPath = storedPath, Sha256 = hash, AddedAt = addedAt, AddedByUserId = AppSession.CurrentUser.UserId, IsIntegrityVerified = true };
        }

        public static EvidenceRecord AddUserComplaintProof(string caseId, string sourcePath) => AddEvidenceFile(caseId, sourcePath);

        public static EvidenceRecord? GetEvidenceById(string evidenceId)
        {
            if (!AppSession.IsSignedIn) return null;
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT EvidenceId, CaseId, FileName, StoredPath, Sha256, AddedAt, AddedByUserId FROM Evidence WHERE EvidenceId = $id LIMIT 1;";
                command.Parameters.AddWithValue("$id", evidenceId.Trim());
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    var ev = new EvidenceRecord
                    {
                        EvidenceId = reader.GetString(0),
                        CaseId = reader.IsDBNull(1) ? "GENERAL" : reader.GetString(1),
                        FileName = reader.GetString(2),
                        StoredPath = reader.GetString(3),
                        Sha256 = reader.GetString(4),
                        AddedAt = reader.GetString(5),
                        AddedByUserId = reader.IsDBNull(6) ? (long?)null : reader.GetInt64(6)
                    };
                    ev.IsIntegrityVerified = File.Exists(ev.StoredPath) && string.Equals(ComputeSha256(ev.StoredPath), ev.Sha256, StringComparison.OrdinalIgnoreCase);
                    return ev;
                }
            }
        }

        public static List<EvidenceRecord> GetEvidenceByCaseId(string caseId)
        {
            List<EvidenceRecord> list = new List<EvidenceRecord>();
            if (string.IsNullOrWhiteSpace(caseId)) return list;
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT EvidenceId, CaseId, FileName, StoredPath, Sha256, AddedAt, AddedByUserId FROM Evidence WHERE CaseId = $caseId ORDER BY AddedAt DESC;";
                command.Parameters.AddWithValue("$caseId", caseId.Trim());
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var ev = new EvidenceRecord
                        {
                            EvidenceId = reader.GetString(0),
                            CaseId = reader.GetString(1),
                            FileName = reader.GetString(2),
                            StoredPath = reader.GetString(3),
                            Sha256 = reader.GetString(4),
                            AddedAt = reader.GetString(5),
                            AddedByUserId = reader.IsDBNull(6) ? (long?)null : reader.GetInt64(6)
                        };
                        ev.IsIntegrityVerified = File.Exists(ev.StoredPath) && string.Equals(ComputeSha256(ev.StoredPath), ev.Sha256, StringComparison.OrdinalIgnoreCase);
                        list.Add(ev);
                    }
                }
            }
            return list;
        }

        public static string ComputeSha256(string filePath)
        {
            using (SHA256 sha256 = SHA256.Create())
            using (FileStream stream = File.OpenRead(filePath))
                return Convert.ToHexString(sha256.ComputeHash(stream));
        }

        // ==================== ROLE-SPECIFIC REPORTS (Q3) ====================

        public static SystemReport GetSystemReport()
        {
            AppSession.DemandRole("OFFICER", "SUPERADMIN");
            SystemReport report = new SystemReport();
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                if (AppSession.CurrentUser!.Role == "SUPERADMIN")
                {
                    report.ReportScope = "Global CyberShield System Analytics (All Zones)";
                    command.CommandText = @"SELECT
 (SELECT COUNT(*) FROM Complaints),
 (SELECT COUNT(*) FROM Evidence),
 (SELECT COUNT(*) FROM Complaints WHERE Status = 'Submitted'),
 (SELECT COUNT(*) FROM Complaints WHERE Status = 'In Progress'),
 (SELECT COUNT(*) FROM Complaints WHERE Status = 'Resolved'),
 (SELECT COUNT(*) FROM Complaints WHERE Status = 'Closed'),
 (SELECT COUNT(*) FROM Complaints WHERE Status = 'Withdrawn'),
 (SELECT COALESCE(SUM(AmountLost), 0) FROM Complaints),
 (SELECT COUNT(*) FROM Complaints WHERE Priority = 'High'),
 (SELECT COUNT(*) FROM Complaints WHERE IsGoldenHour = 1);";
                }
                else
                {
                    string myTag = AppSession.GetOfficerTag(AppSession.CurrentUser);
                    report.ReportScope = $"Officer Personal Performance Report — {myTag}";
                    command.CommandText = @"SELECT
 (SELECT COUNT(*) FROM Complaints WHERE AssignedOfficer = $tag),
 (SELECT COUNT(*) FROM Evidence WHERE CaseId IN (SELECT CaseId FROM Complaints WHERE AssignedOfficer = $tag)),
 (SELECT COUNT(*) FROM Complaints WHERE AssignedOfficer = $tag AND Status = 'Submitted'),
 (SELECT COUNT(*) FROM Complaints WHERE AssignedOfficer = $tag AND Status = 'In Progress'),
 (SELECT COUNT(*) FROM Complaints WHERE AssignedOfficer = $tag AND Status = 'Resolved'),
 (SELECT COUNT(*) FROM Complaints WHERE AssignedOfficer = $tag AND Status = 'Closed'),
 (SELECT COUNT(*) FROM Complaints WHERE AssignedOfficer = $tag AND Status = 'Withdrawn'),
 (SELECT COALESCE(SUM(AmountLost), 0) FROM Complaints WHERE AssignedOfficer = $tag),
 (SELECT COUNT(*) FROM Complaints WHERE AssignedOfficer = $tag AND Priority = 'High'),
 (SELECT COUNT(*) FROM Complaints WHERE AssignedOfficer = $tag AND IsGoldenHour = 1);";
                    command.Parameters.AddWithValue("$tag", myTag);
                }

                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        report.TotalComplaints = reader.GetInt32(0);
                        report.TotalEvidence = reader.GetInt32(1);
                        report.Submitted = reader.GetInt32(2);
                        report.InProgress = reader.GetInt32(3);
                        report.Resolved = reader.GetInt32(4);
                        report.Closed = reader.GetInt32(5);
                        report.Withdrawn = reader.GetInt32(6);
                        report.TotalAmountLost = reader.GetDecimal(7);
                        report.HighPriorityCases = reader.GetInt32(8);
                        report.GoldenHourCases = reader.GetInt32(9);
                    }
                }
            }
            return report;
        }

        // ==================== SUPERADMIN SEPARATED MANAGEMENT & TIPS ====================

        public static List<AppUser> GetManageableAccounts()
        {
            AppSession.DemandRole("SUPERADMIN");
            List<AppUser> users = new List<AppUser>();
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT a.UserId, a.FullName, a.Email, a.Mobile, a.Role, a.AccountStatus, a.EmployeeId, COALESCE(a.CyberZone, 'Dehradun Cyber Cell'),
(SELECT COUNT(*) FROM Complaints c WHERE c.AssignedOfficer = (a.FullName || ' (' || a.EmployeeId || ')') AND c.Status NOT IN ('Closed', 'Withdrawn'))
FROM Accounts a WHERE a.Role <> 'SUPERADMIN' ORDER BY a.Role ASC, a.CreatedAt DESC;";
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new AppUser
                        {
                            UserId = reader.GetInt64(0),
                            FullName = reader.GetString(1),
                            Email = reader.GetString(2),
                            Mobile = reader.GetString(3),
                            Role = reader.GetString(4),
                            AccountStatus = reader.GetString(5),
                            EmployeeId = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                            CyberZone = reader.GetString(7),
                            ActiveCaseCount = reader.GetInt32(8)
                        });
                    }
                }
            }
            return users;
        }

        public static List<AppUser> GetActiveOfficers()
        {
            List<AppUser> officers = new List<AppUser>();
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT UserId, FullName, Email, EmployeeId, COALESCE(CyberZone, 'Dehradun Cyber Cell') FROM Accounts WHERE Role = 'OFFICER' AND AccountStatus = 'ACTIVE';";
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        officers.Add(new AppUser
                        {
                            UserId = reader.GetInt64(0),
                            FullName = reader.GetString(1),
                            Email = reader.GetString(2),
                            EmployeeId = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            CyberZone = reader.GetString(4)
                        });
                    }
                }
            }
            return officers;
        }

        public static List<ComplaintRecord> GetPendingAccessRequests()
        {
            AppSession.DemandRole("SUPERADMIN");
            List<ComplaintRecord> list = new List<ComplaintRecord>();
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT " + ComplaintSelectColumns + " FROM Complaints WHERE AccessRequestBy IS NOT NULL AND AccessRequestBy <> '' ORDER BY IsGoldenHour DESC, CreatedAt DESC;";
                using (SqliteDataReader reader = command.ExecuteReader())
                    while (reader.Read()) list.Add(ReadComplaint(reader));
            }
            return list;
        }

        public static void SubmitAnonymousTip(string cyberZone, string category, string suspectInfo, string details)
        {
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = @"INSERT INTO AnonymousTips (CyberZone, Category, SuspectInfo, Details, CreatedAt)
VALUES ($zone, $cat, $suspect, $details, $at);";
                command.Parameters.AddWithValue("$zone", cyberZone.Trim());
                command.Parameters.AddWithValue("$cat", category.Trim());
                command.Parameters.AddWithValue("$suspect", suspectInfo.Trim());
                command.Parameters.AddWithValue("$details", details.Trim());
                command.Parameters.AddWithValue("$at", UtcText(DateTime.UtcNow));
                command.ExecuteNonQuery();
            }
            WriteAudit(null, "ANONYMOUS_TIP_SUBMITTED", $"Anonymous tip filed for {cyberZone}.");
        }

        public static List<AnonymousTipRecord> GetAnonymousTips()
        {
            AppSession.DemandRole("OFFICER", "SUPERADMIN");
            List<AnonymousTipRecord> list = new List<AnonymousTipRecord>();
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                if (AppSession.CurrentUser!.Role == "SUPERADMIN")
                    command.CommandText = "SELECT TipId, CyberZone, Category, SuspectInfo, Details, CreatedAt FROM AnonymousTips ORDER BY TipId DESC;";
                else
                {
                    command.CommandText = "SELECT TipId, CyberZone, Category, SuspectInfo, Details, CreatedAt FROM AnonymousTips WHERE CyberZone = $zone ORDER BY TipId DESC;";
                    command.Parameters.AddWithValue("$zone", AppSession.CurrentUser.CyberZone);
                }
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new AnonymousTipRecord
                        {
                            TipId = reader.GetInt64(0),
                            CyberZone = reader.GetString(1),
                            Category = reader.GetString(2),
                            SuspectInfo = reader.GetString(3),
                            Details = reader.GetString(4),
                            CreatedAt = reader.GetString(5)
                        });
                    }
                }
            }
            return list;
        }

        public static int GetSuspectComplaintCount(string suspectQuery)
        {
            if (string.IsNullOrWhiteSpace(suspectQuery)) return 0;
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM Complaints WHERE SuspectIdentifier = $q;";
                command.Parameters.AddWithValue("$q", suspectQuery.Trim().ToLowerInvariant());
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        public static List<LockedAccountRecord> GetLockedAccounts()
        {
            AppSession.DemandRole("SUPERADMIN");
            List<LockedAccountRecord> list = new List<LockedAccountRecord>();
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Identifier, FailureCount, WindowStart, COALESCE(LockedUntil, 'Not Locked') FROM LoginAttempts ORDER BY FailureCount DESC;";
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new LockedAccountRecord
                        {
                            Identifier = reader.GetString(0),
                            FailureCount = reader.GetInt32(1),
                            WindowStart = reader.GetString(2),
                            LockedUntil = reader.GetString(3)
                        });
                    }
                }
            }
            return list;
        }

        public static void UnlockAccount(string identifier)
        {
            AppSession.DemandRole("SUPERADMIN");
            ClearLoginFailures(identifier);
            WriteAudit(AppSession.CurrentUser!.UserId, "ACCOUNT_UNLOCKED", "Unlocked login attempts for " + identifier + ".");
        }

        public static List<AuditLogRecord> GetAuditLogs()
        {
            AppSession.DemandRole("SUPERADMIN");
            List<AuditLogRecord> logs = new List<AuditLogRecord>();
            using (SqliteConnection connection = OpenConnection())
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT AuditId, ActorUserId, Action, Details, CreatedAt FROM AuditLog ORDER BY AuditId DESC LIMIT 100;";
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        logs.Add(new AuditLogRecord
                        {
                            AuditId = reader.GetInt64(0),
                            ActorUserId = reader.IsDBNull(1) ? (long?)null : reader.GetInt64(1),
                            Action = reader.GetString(2),
                            Details = reader.GetString(3),
                            CreatedAt = reader.GetString(4)
                        });
                    }
                }
            }
            return logs;
        }
    }
}