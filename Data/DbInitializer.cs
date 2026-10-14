using Microsoft.AspNetCore.Identity;
using AcxiomCRM.Models;

namespace AcxiomCRM.Data
{
    public static class DbInitializer
    {
        public static async Task SeedDataAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await context.Database.EnsureCreatedAsync();

            // 1. Seed Roles
            string[] roles = { "Admin", "Manager", "SalesExecutive" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed Users
            var adminUser = await userManager.FindByEmailAsync("admin@acxiomcrm.com");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin@acxiomcrm.com",
                    Email = "admin@acxiomcrm.com",
                    FullName = "System Administrator",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(adminUser, "Admin@12345");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            var managerUser = await userManager.FindByEmailAsync("manager@acxiomcrm.com");
            if (managerUser == null)
            {
                managerUser = new ApplicationUser
                {
                    UserName = "manager@acxiomcrm.com",
                    Email = "manager@acxiomcrm.com",
                    FullName = "Sales Manager",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(managerUser, "Manager@12345");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(managerUser, "Manager");
                }
            }

            var salesUser1 = await userManager.FindByEmailAsync("sales1@acxiomcrm.com");
            if (salesUser1 == null)
            {
                salesUser1 = new ApplicationUser
                {
                    UserName = "sales1@acxiomcrm.com",
                    Email = "sales1@acxiomcrm.com",
                    FullName = "John Sales",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(salesUser1, "Sales@12345");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(salesUser1, "SalesExecutive");
                }
            }

            var salesUser2 = await userManager.FindByEmailAsync("sales2@acxiomcrm.com");
            if (salesUser2 == null)
            {
                salesUser2 = new ApplicationUser
                {
                    UserName = "sales2@acxiomcrm.com",
                    Email = "sales2@acxiomcrm.com",
                    FullName = "Sarah Jenkins",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(salesUser2, "Sales@12345");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(salesUser2, "SalesExecutive");
                }
            }

            // 3. Seed Customers
            if (!context.Customers.Any())
            {
                var customers = new List<Customer>
                {
                    new Customer
                    {
                        CustomerCode = "CUST-1001",
                        CustomerName = "Apex Global Technologies",
                        Email = "contact@apextech.com",
                        Phone = "9876543210",
                        CompanyName = "Apex Global Inc.",
                        Address = "45 Tech Boulevard, Cyber City",
                        City = "Bangalore",
                        State = "Karnataka",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddMonths(-3),
                        OwnerId = salesUser1.Id,
                        CreatedBy = adminUser.Id
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1002",
                        CustomerName = "Starlight Retailers",
                        Email = "info@starlightretail.com",
                        Phone = "9823456789",
                        CompanyName = "Starlight Retails Pvt Ltd",
                        Address = "12 Market Street",
                        City = "Mumbai",
                        State = "Maharashtra",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddMonths(-2),
                        OwnerId = salesUser2.Id,
                        CreatedBy = adminUser.Id
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1003",
                        CustomerName = "Nexus Financial Services",
                        Email = "finance@nexuscorp.com",
                        Phone = "9123456780",
                        CompanyName = "Nexus Finance Ltd",
                        Address = "88 Wall Street Square",
                        City = "New Delhi",
                        State = "Delhi",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddMonths(-1),
                        OwnerId = salesUser1.Id,
                        CreatedBy = adminUser.Id
                    }
                };

                context.Customers.AddRange(customers);
                await context.SaveChangesAsync();

                // 4. Seed Leads
                var leads = new List<Lead>
                {
                    new Lead
                    {
                        LeadCode = "LEAD-1001",
                        LeadName = "David Miller",
                        Email = "david.miller@zenithhealth.org",
                        Phone = "9988776655",
                        CompanyName = "Zenith Healthcare",
                        Source = "Website",
                        Status = "Qualified",
                        Priority = "High",
                        ExpectedValue = 45000,
                        Notes = "Looking for enterprise CRM implementation across 5 hospitals.",
                        CreatedDate = DateTime.UtcNow.AddDays(-15),
                        AssignedTo = salesUser1.Id
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1002",
                        LeadName = "Emily Watson",
                        Email = "emily@orbitmedia.co",
                        Phone = "9776655443",
                        CompanyName = "Orbit Media Group",
                        Source = "Referral",
                        Status = "Contacted",
                        Priority = "Medium",
                        ExpectedValue = 28000,
                        Notes = "Initial introductory call completed. Demo scheduled next Tuesday.",
                        CreatedDate = DateTime.UtcNow.AddDays(-8),
                        AssignedTo = salesUser2.Id
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1003",
                        LeadName = "Robert Chen",
                        Email = "robert@chenlogistics.com",
                        Phone = "9665544332",
                        CompanyName = "Chen Logistics Supply",
                        Source = "Cold Call",
                        Status = "New",
                        Priority = "Low",
                        ExpectedValue = 15000,
                        Notes = "Requested brochure and pricing tiers.",
                        CreatedDate = DateTime.UtcNow.AddDays(-2),
                        AssignedTo = salesUser1.Id
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1004",
                        LeadName = "Sophia Martinez",
                        Email = "smartinez@solarenergy.net",
                        Phone = "9554433221",
                        CompanyName = "Solaris Dynamics",
                        Source = "Social Media",
                        Status = "Converted",
                        Priority = "High",
                        ExpectedValue = 60000,
                        Notes = "Converted into customer Apex Global Tech.",
                        CreatedDate = DateTime.UtcNow.AddMonths(-1),
                        AssignedTo = salesUser1.Id,
                        ConvertedCustomerId = customers[0].CustomerId
                    }
                };

                context.Leads.AddRange(leads);
                await context.SaveChangesAsync();

                // 5. Seed Opportunities
                var opportunities = new List<Opportunity>
                {
                    new Opportunity
                    {
                        OpportunityName = "Apex Cloud Migration Enterprise Deal",
                        CustomerId = customers[0].CustomerId,
                        Amount = 75000,
                        Stage = "Negotiation",
                        Probability = 80,
                        ExpectedCloseDate = DateTime.Today.AddDays(14),
                        Status = "Open",
                        Source = "Existing Client",
                        Notes = "Final contract terms under legal review.",
                        CreatedDate = DateTime.UtcNow.AddDays(-30),
                        AssignedTo = salesUser1.Id
                    },
                    new Opportunity
                    {
                        OpportunityName = "Starlight Omnichannel POS Integration",
                        CustomerId = customers[1].CustomerId,
                        Amount = 52000,
                        Stage = "Proposal",
                        Probability = 50,
                        ExpectedCloseDate = DateTime.Today.AddDays(25),
                        Status = "Open",
                        Source = "Referral",
                        Notes = "Architecture proposal submitted.",
                        CreatedDate = DateTime.UtcNow.AddDays(-20),
                        AssignedTo = salesUser2.Id
                    },
                    new Opportunity
                    {
                        OpportunityName = "Nexus Risk Management Module",
                        CustomerId = customers[2].CustomerId,
                        Amount = 120000,
                        Stage = "Won",
                        Probability = 100,
                        ExpectedCloseDate = DateTime.Today.AddDays(-5),
                        Status = "Won",
                        Source = "Direct Sale",
                        Notes = "Contract signed and deposit received.",
                        CreatedDate = DateTime.UtcNow.AddMonths(-2),
                        AssignedTo = salesUser1.Id
                    },
                    new Opportunity
                    {
                        OpportunityName = "Zenith Hospital Management System",
                        LeadId = leads[0].LeadId,
                        Amount = 90000,
                        Stage = "Qualification",
                        Probability = 30,
                        ExpectedCloseDate = DateTime.Today.AddDays(45),
                        Status = "Open",
                        Source = "Website",
                        Notes = "Requirements gathering workshop pending.",
                        CreatedDate = DateTime.UtcNow.AddDays(-10),
                        AssignedTo = salesUser1.Id
                    }
                };

                context.Opportunities.AddRange(opportunities);
                await context.SaveChangesAsync();

                // 6. Seed FollowUps
                var followUps = new List<FollowUp>
                {
                    new FollowUp
                    {
                        CustomerId = customers[0].CustomerId,
                        OpportunityId = opportunities[0].OpportunityId,
                        FollowUpDate = DateTime.Today.AddDays(2),
                        FollowUpType = "Meeting",
                        Subject = "Contract review meeting with Apex Legal Team",
                        Remarks = "Discuss SLA clauses and payment milestones.",
                        Status = "Planned",
                        AssignedTo = salesUser1.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-3)
                    },
                    new FollowUp
                    {
                        CustomerId = customers[1].CustomerId,
                        OpportunityId = opportunities[1].OpportunityId,
                        FollowUpDate = DateTime.Today.AddDays(4),
                        FollowUpType = "Call",
                        Subject = "Follow up on proposal feedback with Starlight CTO",
                        Remarks = "Answer technical queries regarding API latency.",
                        Status = "Planned",
                        AssignedTo = salesUser2.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-2)
                    },
                    new FollowUp
                    {
                        LeadId = leads[1].LeadId,
                        FollowUpDate = DateTime.Today.AddDays(-1), // Past planned follow-up to demonstrate overdue
                        FollowUpType = "Call",
                        Subject = "Overdue follow-up with Orbit Media",
                        Remarks = "Call Emily to confirm demo attendees.",
                        Status = "Planned",
                        AssignedTo = salesUser2.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-5)
                    },
                    new FollowUp
                    {
                        CustomerId = customers[2].CustomerId,
                        FollowUpDate = DateTime.Today.AddDays(-7),
                        FollowUpType = "Email",
                        Subject = "Sent kickoff onboarding schedule",
                        Remarks = "Client confirmed receipt and team roster.",
                        Status = "Completed",
                        CompletedDate = DateTime.Today.AddDays(-7),
                        AssignedTo = salesUser1.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-10)
                    }
                };

                context.FollowUps.AddRange(followUps);
                await context.SaveChangesAsync();

                // 7. Seed Activities
                var activities = new List<Activity>
                {
                    new Activity
                    {
                        ActivityType = "Call",
                        Subject = "Discovery call with David Miller",
                        Description = "Identified key requirements for 5 hospital branches.",
                        ActivityDate = DateTime.UtcNow.AddDays(-12),
                        LeadId = leads[0].LeadId,
                        AssignedTo = salesUser1.Id,
                        Status = "Completed"
                    },
                    new Activity
                    {
                        ActivityType = "Meeting",
                        Subject = "On-site pitch presentation at Apex Global",
                        Description = "Presented executive pitch deck to VP and procurement head.",
                        ActivityDate = DateTime.UtcNow.AddDays(-8),
                        CustomerId = customers[0].CustomerId,
                        OpportunityId = opportunities[0].OpportunityId,
                        AssignedTo = salesUser1.Id,
                        Status = "Completed"
                    },
                    new Activity
                    {
                        ActivityType = "Email",
                        Subject = "Sent revised commercial proposal",
                        Description = "Included tiered volume discounts as requested.",
                        ActivityDate = DateTime.UtcNow.AddDays(-3),
                        CustomerId = customers[1].CustomerId,
                        OpportunityId = opportunities[1].OpportunityId,
                        AssignedTo = salesUser2.Id,
                        Status = "Completed"
                    }
                };

                context.Activities.AddRange(activities);
                await context.SaveChangesAsync();

                // 8. Seed Audit Logs
                var auditLogs = new List<AuditLog>
                {
                    new AuditLog
                    {
                        UserId = adminUser.Id,
                        UserName = adminUser.UserName,
                        Module = "Authentication",
                        Action = "Login",
                        Result = "Success",
                        Details = "System administrator initialized platform configuration.",
                        IpAddress = "127.0.0.1",
                        Timestamp = DateTime.UtcNow.AddDays(-30)
                    },
                    new AuditLog
                    {
                        UserId = adminUser.Id,
                        UserName = adminUser.UserName,
                        Module = "UserManagement",
                        Action = "Role Change",
                        Result = "Success",
                        Details = "Assigned SalesExecutive role to sales1@acxiomcrm.com and sales2@acxiomcrm.com.",
                        IpAddress = "127.0.0.1",
                        Timestamp = DateTime.UtcNow.AddDays(-29)
                    },
                    new AuditLog
                    {
                        UserId = salesUser1.Id,
                        UserName = salesUser1.UserName,
                        Module = "Lead",
                        Action = "Create",
                        EntityName = "Lead",
                        RecordId = "LEAD-1001",
                        Result = "Success",
                        Details = "Captured inbound lead David Miller (Zenith Healthcare).",
                        IpAddress = "127.0.0.1",
                        Timestamp = DateTime.UtcNow.AddDays(-15)
                    },
                    new AuditLog
                    {
                        UserId = salesUser1.Id,
                        UserName = salesUser1.UserName,
                        Module = "Lead",
                        Action = "Convert",
                        EntityName = "Lead",
                        RecordId = "LEAD-1004",
                        Result = "Success",
                        Details = "Converted lead Sophia Martinez to Customer Apex Global Technologies.",
                        IpAddress = "127.0.0.1",
                        Timestamp = DateTime.UtcNow.AddMonths(-1)
                    }
                };

                context.AuditLogs.AddRange(auditLogs);
                await context.SaveChangesAsync();
            }
        }
    }
}
