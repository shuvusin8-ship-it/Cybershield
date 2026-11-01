using Cybershield.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;

namespace Cybershield.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (!DatabaseService.HasSuperAdmin())
                return RedirectToAction("Setup");

            if (!AppSession.IsSignedIn)
                return RedirectToAction("Login");

            var user = AppSession.CurrentUser!;
            if (user.Role == "USER")
            {
                ViewBag.UserComplaints = DatabaseService.GetComplaintsByUserId(user.UserId);
            }
            else if (user.Role == "OFFICER")
            {
                ViewBag.IncomingZoneCases = DatabaseService.GetIncomingZoneCasesForOfficer(user);
                ViewBag.MyAssignedCasesCount = DatabaseService.GetAllComplaints().Count;
            }
            else if (user.Role == "SUPERADMIN")
            {
                var allCases = DatabaseService.GetAllComplaints();
                ViewBag.UnassignedCasesCount = allCases.FindAll(c => c.AssignedOfficer == "Unassigned").Count;
                ViewBag.PendingRequestsCount = DatabaseService.GetPendingAccessRequests().Count;
                ViewBag.GoldenHourCount = allCases.FindAll(c => c.IsGoldenHour && c.Status == "Submitted").Count;
            }

            return View(user);
        }

        [HttpGet]
        public IActionResult Setup()
        {
            if (DatabaseService.HasSuperAdmin()) return RedirectToAction("Login");
            return View();
        }

        [HttpPost]
        public IActionResult Setup(string fullName, string email, string mobile, string password, string confirmPassword)
        {
            if (password != confirmPassword) { ViewBag.Error = "Passwords do not match."; return View(); }
            try
            {
                DatabaseService.CreateInitialSuperAdmin(fullName, email, mobile, password);
                TempData["Success"] = "Initial SuperAdmin created! Please sign in.";
                return RedirectToAction("Login");
            }
            catch (Exception ex) { ViewBag.Error = ex.Message; return View(); }
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (!DatabaseService.HasSuperAdmin()) return RedirectToAction("Setup");
            return View();
        }

        [HttpPost]
        public IActionResult Login(string identifier, string password)
        {
            try
            {
                int lockSeconds = DatabaseService.GetLockoutRemainingSeconds(identifier ?? "");
                if (lockSeconds > 0)
                {
                    ViewBag.Error = $"Account temporarily locked. Try again in {Math.Ceiling(lockSeconds / 60.0)} minutes.";
                    return View();
                }

                AppUser? user = DatabaseService.Authenticate(identifier ?? "", password ?? "");
                if (user == null)
                {
                    ViewBag.Error = "Invalid login credentials or inactive account.";
                    return View();
                }

                AppSession.SignIn(user);
                return RedirectToAction("Index");
            }
            catch (Exception ex) { ViewBag.Error = ex.Message; return View(); }
        }

        public IActionResult Logout()
        {
            AppSession.SignOut();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(string fullName, string email, string mobile, string password, string confirmPassword)
        {
            if (password != confirmPassword) { ViewBag.Error = "Passwords do not match."; return View(); }
            try
            {
                DatabaseService.RegisterUser(fullName, email, mobile, password);
                TempData["Success"] = "User account registered! Please login.";
                return RedirectToAction("Login");
            }
            catch (Exception ex) { ViewBag.Error = ex.Message; return View(); }
        }

        [HttpGet]
        public IActionResult OfficerRegister() => View();

        [HttpPost]
        public IActionResult OfficerRegister(string fullName, string email, string mobile, string employeeId, string token, string password, string confirmPassword)
        {
            if (password != confirmPassword) { ViewBag.Error = "Passwords do not match."; return View(); }
            try
            {
                DatabaseService.RegisterOfficer(fullName, email, mobile, employeeId, token, password);
                TempData["Success"] = "Officer account activated! Please login.";
                return RedirectToAction("Login");
            }
            catch (Exception ex) { ViewBag.Error = ex.Message; return View(); }
        }

        // Anonymous Cyber Tip-Off (Whistleblower)
        [HttpGet]
        public IActionResult AnonymousTip() => View();

        [HttpPost]
        public IActionResult AnonymousTip(string cyberZone, string category, string suspectInfo, string details)
        {
            DatabaseService.SubmitAnonymousTip(cyberZone, category, suspectInfo, details);
            ViewBag.Success = "Your anonymous cyber tip-off has been securely transmitted to the selected Cyber Cell.";
            return View();
        }

        [HttpGet]
        public IActionResult ViewTips()
        {
            if (!AppSession.HasRole("OFFICER", "SUPERADMIN")) return RedirectToAction("Index");
            return View(DatabaseService.GetAnonymousTips());
        }

        // User Complaint Registration
        [HttpGet]
        public IActionResult Complaint()
        {
            if (!AppSession.HasRole("USER")) return RedirectToAction("Index");
            return View();
        }

        [HttpPost]
        public IActionResult Complaint(string crimeType, DateTime complaintDate, string description,
            string? suspectIdentifier, decimal amountLost, string? transactionId, string? cyberZone, IFormFile? proofFile)
        {
            if (!AppSession.HasRole("USER")) return RedirectToAction("Index");
            try
            {
                string caseId = DatabaseService.CreateComplaint(
                    AppSession.CurrentUser!.FullName,
                    AppSession.CurrentUser.Email,
                    crimeType,
                    complaintDate,
                    description,
                    AppSession.CurrentUser.UserId,
                    suspectIdentifier,
                    amountLost,
                    transactionId,
                    cyberZone);

                string proofMsg = "";
                if (proofFile != null && proofFile.Length > 0)
                {
                    string tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(proofFile.FileName));
                    try
                    {
                        using (var stream = new FileStream(tempPath, FileMode.Create))
                            proofFile.CopyTo(stream);
                        var ev = DatabaseService.AddUserComplaintProof(caseId, tempPath);
                        proofMsg = $" | Digital Proof Linked ({ev.EvidenceId})";
                    }
                    finally
                    {
                        if (System.IO.File.Exists(tempPath)) System.IO.File.Delete(tempPath);
                    }
                }

                ViewBag.Success = $"Complaint registered! Case ID: {caseId} — Routed to {cyberZone}{proofMsg}";
            }
            catch (Exception ex) { ViewBag.Error = ex.Message; }
            return View();
        }

        // Unified Check Status (Strict RBAC + Evidence List)
        [HttpGet]
        [HttpPost]
        public IActionResult CheckStatus(string? caseId)
        {
            if (!AppSession.IsSignedIn) return RedirectToAction("Login");
            if (!string.IsNullOrWhiteSpace(caseId))
            {
                var complaint = DatabaseService.GetComplaintByCaseId(caseId, AppSession.CurrentUser!);
                if (complaint == null)
                    ViewBag.Error = "ACCESS DENIED or Case Not Found: You can only view cases that belong to you or are officially assigned/shared with you.";
                else
                {
                    ViewBag.Complaint = complaint;
                    ViewBag.EvidenceList = DatabaseService.GetEvidenceByCaseId(complaint.CaseId);
                }
            }
            return View();
        }

        // Download / View Stored Evidence File (Fixes Evidence Not Showing Issue)
        [HttpGet]
        public IActionResult DownloadEvidence(string evidenceId)
        {
            if (!AppSession.IsSignedIn) return RedirectToAction("Login");
            var ev = DatabaseService.GetEvidenceById(evidenceId);
            if (ev == null || !System.IO.File.Exists(ev.StoredPath))
                return NotFound("Evidence file not found on server.");

            // Check permission for the linked case
            var c = DatabaseService.GetComplaintByCaseId(ev.CaseId, AppSession.CurrentUser!);
            if (c == null && AppSession.CurrentUser!.Role != "SUPERADMIN")
                return Unauthorized("You do not have permission to view this evidence.");

            byte[] fileBytes = System.IO.File.ReadAllBytes(ev.StoredPath);
            return File(fileBytes, "application/octet-stream", ev.FileName);
        }

        // View & Investigate Cases
        [HttpGet]
        public IActionResult ViewCases(string? filterCrime, string? filterStatus, string? searchCaseId)
        {
            if (!AppSession.HasRole("OFFICER", "SUPERADMIN")) return RedirectToAction("Index");
            var cases = DatabaseService.GetAllComplaints();

            var suspectCounts = new Dictionary<string, int>();
            var caseEvidenceMap = new Dictionary<string, List<EvidenceRecord>>();
            foreach (var c in cases)
            {
                if (!string.IsNullOrWhiteSpace(c.SuspectIdentifier))
                    suspectCounts[c.SuspectIdentifier] = suspectCounts.GetValueOrDefault(c.SuspectIdentifier, 0) + 1;
                caseEvidenceMap[c.CaseId] = DatabaseService.GetEvidenceByCaseId(c.CaseId);
            }
            ViewBag.SuspectCounts = suspectCounts;
            ViewBag.CaseEvidenceMap = caseEvidenceMap;
            ViewBag.ActiveOfficers = DatabaseService.GetActiveOfficers();

            if (!string.IsNullOrWhiteSpace(filterCrime))
                cases = cases.FindAll(c => c.CrimeType == filterCrime);
            if (!string.IsNullOrWhiteSpace(filterStatus))
                cases = cases.FindAll(c => c.Status == filterStatus);
            if (!string.IsNullOrWhiteSpace(searchCaseId))
                cases = cases.FindAll(c => c.CaseId.Contains(searchCaseId.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                           c.SuspectIdentifier.Contains(searchCaseId.Trim(), StringComparison.OrdinalIgnoreCase));

            return View(cases);
        }

        // Officer Requests Access to Jurisdiction Case
        [HttpPost]
        public IActionResult RequestAccess(string caseId)
        {
            if (!AppSession.HasRole("OFFICER")) return RedirectToAction("Index");
            if (DatabaseService.RequestCaseAccess(caseId))
                TempData["Success"] = $"Investigation access request for {caseId} sent to SuperAdmin for approval.";
            else
                TempData["Error"] = "Could not request access.";
            return RedirectToAction("Index");
        }

        // Officer Updates Case Status (Only Primary Assigned Officer)
        [HttpPost]
        public IActionResult UpdateCaseStatus(string caseId, string status, string? remarks)
        {
            if (!AppSession.HasRole("OFFICER")) return RedirectToAction("Index");
            try
            {
                if (DatabaseService.UpdateComplaintStatus(caseId, status, remarks))
                    TempData["Success"] = $"Case {caseId} updated to '{status}'. Remark saved to timeline.";
                else
                    TempData["Error"] = "Only the primary assigned officer can change case status.";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction("ViewCases");
        }

        // Officer Shares Case with Co-Investigator
        [HttpPost]
        public IActionResult ShareCase(string caseId, string targetOfficerTag, string shareReason)
        {
            if (!AppSession.HasRole("OFFICER")) return RedirectToAction("Index");
            try
            {
                if (DatabaseService.RequestCaseShare(caseId, targetOfficerTag, shareReason))
                    TempData["Success"] = $"Share request for {caseId} sent to SuperAdmin for security approval.";
                else
                    TempData["Error"] = "Only the primary assigned officer can initiate case sharing.";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction("ViewCases");
        }

        // SuperAdmin Approves/Rejects Access or Share Request
        [HttpPost]
        public IActionResult ProcessRequest(string caseId, bool approve)
        {
            if (!AppSession.HasRole("SUPERADMIN")) return RedirectToAction("Index");
            DatabaseService.ProcessAccessRequest(caseId, approve);
            TempData["Success"] = approve ? $"Request for {caseId} APPROVED." : $"Request for {caseId} REJECTED.";
            return RedirectToAction("ManageOfficers");
        }

        // SuperAdmin Direct Assign / Emergency Re-Assign
        [HttpPost]
        public IActionResult AssignOfficer(string caseId, string officerInfo)
        {
            if (!AppSession.HasRole("SUPERADMIN")) return RedirectToAction("Index");
            DatabaseService.AssignCaseToOfficer(caseId, officerInfo);
            TempData["Success"] = $"Case {caseId} assigned/re-assigned to {officerInfo} (removed from previous officer).";
            return RedirectToAction("ViewCases");
        }

        // Official Section 91 CrPC / 94 BNSS Bank & Telecom Notice Generator
        [HttpGet]
        public IActionResult LegalNotice(string caseId)
        {
            if (!AppSession.HasRole("OFFICER", "SUPERADMIN")) return RedirectToAction("Index");
            var c = DatabaseService.GetComplaintByCaseId(caseId, AppSession.CurrentUser!);
            if (c == null) return RedirectToAction("ViewCases");
            return View(c);
        }

        [HttpGet]
        public IActionResult Reports()
        {
            if (!AppSession.HasRole("OFFICER", "SUPERADMIN")) return RedirectToAction("Index");
            return View(DatabaseService.GetSystemReport());
        }

        [HttpGet]
        public IActionResult ManageOfficers()
        {
            if (!AppSession.HasRole("SUPERADMIN")) return RedirectToAction("Index");
            ViewBag.PendingRequests = DatabaseService.GetPendingAccessRequests();
            return View(DatabaseService.GetManageableAccounts());
        }

        [HttpPost]
        public IActionResult CreateInvite(string officialEmail, string employeeId, string cyberZone, int expiryHours)
        {
            if (!AppSession.HasRole("SUPERADMIN")) return RedirectToAction("Index");
            try
            {
                string token = DatabaseService.CreateOfficerInvitation(officialEmail, employeeId, cyberZone, expiryHours);
                TempData["GeneratedToken"] = token;
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction("ManageOfficers");
        }

        [HttpPost]
        public IActionResult ChangeAccountStatus(long userId, string status)
        {
            if (!AppSession.HasRole("SUPERADMIN")) return RedirectToAction("Index");
            try
            {
                DatabaseService.SetAccountStatus(userId, status);
                TempData["Success"] = $"Account #{userId} status changed to {status}.";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction("ManageOfficers");
        }

        [HttpGet]
        public IActionResult AddEvidence()
        {
            if (!AppSession.HasRole("OFFICER", "SUPERADMIN")) return RedirectToAction("Index");
            ViewBag.AllCases = DatabaseService.GetAllComplaints();
            return View();
        }

        [HttpPost]
        public IActionResult AddEvidence(string caseId, IFormFile evidenceFile)
        {
            if (!AppSession.HasRole("OFFICER", "SUPERADMIN")) return RedirectToAction("Index");
            if (string.IsNullOrWhiteSpace(caseId) || evidenceFile == null || evidenceFile.Length == 0)
            {
                ViewBag.Error = "Please select an assigned Case ID and an evidence file.";
                ViewBag.AllCases = DatabaseService.GetAllComplaints();
                return View();
            }

            string tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(evidenceFile.FileName));
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.Create))
                    evidenceFile.CopyTo(stream);
                ViewBag.Evidence = DatabaseService.AddEvidenceFile(caseId, tempPath);
            }
            catch (Exception ex) { ViewBag.Error = ex.Message; }
            finally { if (System.IO.File.Exists(tempPath)) System.IO.File.Delete(tempPath); }
            ViewBag.AllCases = DatabaseService.GetAllComplaints();
            return View();
        }

        [HttpGet]
        public IActionResult VerifyEvidence()
        {
            if (!AppSession.HasRole("OFFICER", "SUPERADMIN")) return RedirectToAction("Index");
            return View();
        }

        [HttpPost]
        public IActionResult VerifyEvidence(string evidenceId, IFormFile fileToVerify)
        {
            if (!AppSession.HasRole("OFFICER", "SUPERADMIN")) return RedirectToAction("Index");
            if (string.IsNullOrWhiteSpace(evidenceId) || fileToVerify == null || fileToVerify.Length == 0)
            {
                ViewBag.Error = "Enter Evidence ID and select file.";
                return View();
            }

            string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                EvidenceRecord? saved = DatabaseService.GetEvidenceById(evidenceId.Trim());
                if (saved == null) { ViewBag.Error = "Evidence ID not found."; return View(); }

                using (var stream = new FileStream(tempPath, FileMode.Create))
                    fileToVerify.CopyTo(stream);

                string currentHash = DatabaseService.ComputeSha256(tempPath);
                ViewBag.IsMatch = string.Equals(currentHash, saved.Sha256, StringComparison.OrdinalIgnoreCase);
                ViewBag.EvidenceId = saved.EvidenceId;
                ViewBag.SavedHash = saved.Sha256;
                ViewBag.CurrentHash = currentHash;
            }
            catch (Exception ex) { ViewBag.Error = ex.Message; }
            finally { if (System.IO.File.Exists(tempPath)) System.IO.File.Delete(tempPath); }
            return View();
        }

        [HttpGet]
        public IActionResult PrintReceipt(string caseId)
        {
            if (!AppSession.IsSignedIn) return RedirectToAction("Login");
            var complaint = DatabaseService.GetComplaintByCaseId(caseId, AppSession.CurrentUser!);
            if (complaint == null) return RedirectToAction("Index");
            return View(complaint);
        }

        [HttpGet]
        public IActionResult AuditLogs()
        {
            if (!AppSession.HasRole("SUPERADMIN")) return RedirectToAction("Index");
            return View(DatabaseService.GetAuditLogs());
        }

        [HttpGet]
        public IActionResult SuspectLookup(string? query)
        {
            if (!AppSession.IsSignedIn) return RedirectToAction("Login");
            if (!string.IsNullOrWhiteSpace(query))
            {
                ViewBag.Query = query.Trim();
                ViewBag.Count = DatabaseService.GetSuspectComplaintCount(query);
            }
            return View();
        }

        [HttpGet]
        public IActionResult LockedAccounts()
        {
            if (!AppSession.HasRole("SUPERADMIN")) return RedirectToAction("Index");
            return View(DatabaseService.GetLockedAccounts());
        }

        [HttpPost]
        public IActionResult UnlockAccount(string identifier)
        {
            if (!AppSession.HasRole("SUPERADMIN")) return RedirectToAction("Index");
            DatabaseService.UnlockAccount(identifier);
            TempData["Success"] = $"Unlocked login attempts for {identifier}.";
            return RedirectToAction("LockedAccounts");
        }

        [HttpPost]
        public IActionResult WithdrawComplaint(string caseId)
        {
            if (!AppSession.HasRole("USER")) return RedirectToAction("Index");
            if (DatabaseService.WithdrawComplaint(caseId, AppSession.CurrentUser!.UserId))
                TempData["Success"] = $"Complaint {caseId} has been withdrawn.";
            else
                TempData["Error"] = "Only 'Submitted' complaints can be withdrawn.";
            return RedirectToAction("CheckStatus", new { caseId });
        }

        [HttpPost]
        public IActionResult AddFollowUp(string caseId, string additionalNote)
        {
            if (!AppSession.HasRole("USER")) return RedirectToAction("Index");
            if (DatabaseService.AddComplaintFollowUp(caseId, AppSession.CurrentUser!.UserId, additionalNote))
                TempData["Success"] = "Additional information added to your case.";
            else
                TempData["Error"] = "Could not add follow-up to this case.";
            return RedirectToAction("CheckStatus", new { caseId });
        }

        //  Section 63 BSA (formerly 65B IEA) Digital Evidence Certificate
        [HttpGet]
        public IActionResult EvidenceCertificate(string evidenceId)
        {
            if (!AppSession.IsSignedIn) return RedirectToAction("Login");
            var ev = DatabaseService.GetEvidenceById(evidenceId);
            if (ev == null) return NotFound("Evidence record not found.");

            var c = DatabaseService.GetComplaintByCaseId(ev.CaseId, AppSession.CurrentUser!);
            if (c == null && AppSession.CurrentUser!.Role != "SUPERADMIN")
                return Unauthorized("You do not have permission to generate a certificate for this case.");

            ViewBag.Complaint = c;
            return View(ev);
        }
    }
}