# Cybershield
To provide a simple system where a person can report a cybercrime, submit digital evidence, and allow the admin/police side to manage the complaint and check whether the submitted evidence has been changed.
PROPOSAL DESCRIPTION
Motivation
Many cybercrimes, such as phishing, online financial fraud, identity theft, cyberbullying, ransomware, etc., are increasing at a higher pace than the mechanisms to prosecute them. Victims of such crimes face a fragmented ecosystem of agencies to file a complaint with, complicated forms to fill, and no easy way to track the status of their case. For investigators receiving the complaint, digital evidence such as screenshots or call records are scattered across emails, paper printouts, and phone backups – with no timestamped proof of chain of custody to ensure they cannot be tampered with during the forensic process
By building a platform that victims can use to file a complaint, upload digital evidence of the crime, and track its status – while giving investigators a centralized repository to store all evidence with timestamps and cryptographic hashing – this project tackles both sides of the problem, reducing the time between filing and investigation while improving the quality of evidence that reaches courts.
As internet and smartphone adoption increases, so will cybercrimes reported – and with digital evidence becoming more critical to the prosecution, this system will help both police cybercrime units and college/organisational IT wings that currently lack such tools. The project’s potential impact on users is high, given the current reliance on fragmented digital evidence across emails, phone backups, and printouts, with little evidence tamper-proofing beyond a physical paper trail.

State of the Art/ Current Solution

Current platforms available for filing cybercrime complaints include India’s National Cyber Crime Reporting Portal ( cybercrime.gov.in ), the FBI’s IC3 cybercrime reporting system, and similar portals run by police cybercrime wings across the country/state. While these portals allow victims to file a complaint online, there is limited scope for victims to track the status of their case or for investigators to manage digital evidence with tamper-proofing beyond a physical paper trail. Smaller police stations and college/organisational cybercrime cells may not have such portals and have to rely on manual evidence collection and tracking through paper printouts, emails, and file-sharing platforms, with limited ways to verify that a file has not been altered since discovery.
Our proposed system enhances upon existing solutions by adding cryptographic hashing of digital evidence files, automated case categorization, and tracking reference numbers for both victims and investigators while being a lightweight system that can be deployed for a college project or smaller cybercrime units.

 
 


Project Goals and Milestones
Project Goals:
1. To build a cybercrime complaint-filing system that allows a citizen to file a complaint and receive a reference number
2. To build a digital evidence upload/storage system that allows a victim to upload evidence files and automatically compute their SHA256 hash for tamper-proofing
3. To automatically categorize complaints into financial fraud, phishing, harassment, hacking, etc.
4. To build a module for investigators to update case status, add notes, and maintain chain of custody of digital evidence files uploaded
5. To build a case tracker for victims that allows them to track the status of their complaint using the reference number

Project Approach
Development Methodology:
The project will be developed in an incremental manner, in three phases. 
Phase 1 will be a console application that implements the core business logic (e.g., complaint categorization, evidence hashing) without any UI – to develop and test the underlying logic – before building a database-driven application (ADO.NET/Entity Framework Core) in Phase 2 and an ASP.NET web application in Phase 3.
Design Decisions:
1. The system will be divided into layers for processing complaints, managing evidence tracking of investigators, and notification system
2. Digital evidence files uploaded by the victims will be stored with their SHA256 hash, and the hash along with timestamps will be used in establishing chain of custody
3. An object-oriented approach to develop the console application, e.g., classes Complaint, Evidence, Case, and Investigator, and apply a repository pattern in the second phase to decouple data access logic from the business logic in the application.








System Architecture(High Level Diagram)
Phase 1 – Console Application

1. USER INTERFACE LAYER – Console menu to file a complaint, upload evidence, track a case by reference number
2. BUSINESS LOGIC LAYER
1. Complaint Registration Module – to register a complaint and issue a reference number
2. Evidence Management Module – to accept file upload, hash with SHA256
3. Case Tracking Module – update case status, add investigator notes
4. Chain of Custody Module – verify hash of uploaded evidence, log access to files
5. Notification Module – to notify complainant of case status updates
3. DATA LAYER – In memory dictionaries/lists to store case details, evidence, and status for the console application
4. OUTPUT LAYER – Generator for case report/evidence log in .txt/.json format

Data flow:
Citizen Inputs → Menu Handler → Business Logic Layer → Update Dictionary/Lists → Generate Outputs
The future architecture (Phase 2 & 3) would transition into a database driven application, using an SQL Server/SQLite database (Entity Framework Core) to track evidence files and their hashes for a verifiable chain of custody, as opposed to using in memory lists. The main components of the application to support electronic case reporting are as follows:
1. USER INTERFACE LAYER – Console menu to file a complaint, upload evidence, track a case by reference number
2. BUSINESS LOGIC LAYER
1. Complaint Registration Module – to register a complaint and issue a reference number
2. Evidence Management Module – to accept file upload, hash with SHA256
3. Case Tracking Module – update case status, add investigator notes
4. Chain of Custody Module – verify hash of uploaded evidence, log access to files
5. Notification Module – to notify complainant of case status updates
3. DATA LAYER – In memory dictionaries/lists to store case details, evidence, and status for the console application
4. OUTPUT LAYER – Generator for case report/evidence log in .txt/.json format

Data flow:
Citizen Inputs → Menu Handler → Business Logic Layer → Update Dictionary/Lists → Generate Outputs
The future architecture (Phase 2 & 3) would transition into a database driven application, using an SQL Server/SQLite database (Entity Framework Core) to track evidence files and their hashes for a verifiable chain of custody, as opposed to using in memory lists.Phase 3 will convert the console application into a web-based application using ASP.NET Core MVC/Web API that runs as a client-server system, with login forms for citizens to file a complaint or track a case and investigators to update a case’s status.
Project Outcome /Deliverables
Deliverables for this phase include:
• Complete source code with extensive commenting and documentation
• Console application with working complaint registration, evidence upload/hashing, case tracking, and case report/evidence log generation
• User manual with screenshots and instructions for using the application
• Sample case report and evidence log files to showcase the application’s output format
• Test cases and scenarios
Expected Outcome:
Prove your proficiency in developing applications in C#
Show your understanding of such concepts as variables, methods, loops, conditions, and object-oriented programming
Demonstrate your ability to develop complex logic such as validation, hashing, and file I/O in a real-world project
Assumptions
Users are familiar with using computers and can navigate a console application
The target system has .NET SDK (8.0 or higher) installed to run the application
Complaint categories and corresponding IT Act/IPC sections are configured manually and not fetched from a database
Evidence files uploaded are smaller than a predefined size limit and in standard formats (images, PDF, text, audio)
The console application is single-user and does not handle concurrency


Assumptions
• Users have basic computer literacy and can operate console applications
• .NET SDK (8.0 or higher) and Visual Studio/VS Code are installed on the target system
• Complaint categories and applicable IT Act/IPC sections are pre-configured, not fetched from an external legal database
• Evidence files are below a defined size limit and in common formats (images, PDFs, text, audio)
• Single-user operation, with no concurrent case-filing conflicts, in Phase 1
References 
1. National Cyber Crime Reporting Portal – India : https://cybercrime.gov.in/
2. FBI Internet Crime Complaint Center – FBI IC3 : https://www.ic3.gov/
3. NIST SP 800-86: Guide to Integrating Forensic Techniques into Incident Response
4. Microsoft System.Security.Cryptography (SHA256) : https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.sha256
5. ASP.NET Core Documentation: https://learn.microsoft.com/en-us/asp.net/core/ (Future Use)
