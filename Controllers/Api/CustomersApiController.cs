using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/customers")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},Identity.Application")]
    public class CustomersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;

        public CustomersApiController(
            ApplicationDbContext context,
            ICrmService crmService,
            IAuditService auditService)
        {
            _context = context;
            _crmService = crmService;
            _auditService = auditService;
        }

        /// <summary>
        /// Get list of authorized customers with optional search and status filter
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers([FromQuery] string? search, [FromQuery] string? status)
        {
            var query = _crmService.GetScopedCustomers(User);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(c => c.CustomerName.ToLower().Contains(s) ||
                                         c.Email.ToLower().Contains(s) ||
                                         c.Phone.Contains(s) ||
                                         (c.CompanyName != null && c.CompanyName.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c => c.Status == status);
            }

            var list = await query.OrderByDescending(c => c.CreatedDate)
                .Select(c => new CustomerDto
                {
                    CustomerId = c.CustomerId,
                    CustomerCode = c.CustomerCode,
                    CustomerName = c.CustomerName,
                    Email = c.Email,
                    Phone = c.Phone,
                    CompanyName = c.CompanyName,
                    Address = c.Address,
                    City = c.City,
                    State = c.State,
                    Status = c.Status,
                    CreatedDate = c.CreatedDate,
                    OwnerId = c.OwnerId,
                    OwnerName = c.Owner != null ? c.Owner.FullName : null
                })
                .ToListAsync();

            return Ok(list);
        }

        /// <summary>
        /// Get customer by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerDto>> GetCustomer(int id)
        {
            var c = await _crmService.GetScopedCustomers(User)
                .FirstOrDefaultAsync(x => x.CustomerId == id);

            if (c == null)
            {
                return NotFound(new { message = $"Customer with ID {id} not found or access denied." });
            }

            return Ok(new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                City = c.City,
                State = c.State,
                Status = c.Status,
                CreatedDate = c.CreatedDate,
                OwnerId = c.OwnerId,
                OwnerName = c.Owner != null ? c.Owner.FullName : null
            });
        }

        /// <summary>
        /// Create a new customer
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CustomerDto>> CreateCustomer([FromBody] CreateCustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (isDuplicate, dupReason) = await _crmService.CheckDuplicateCustomerAsync(
                dto.Email, dto.Phone, dto.CustomerName, dto.CompanyName);

            if (isDuplicate)
            {
                return Conflict(new { message = dupReason });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var count = await _context.Customers.CountAsync();
            var customer = new Customer
            {
                CustomerCode = $"CUST-{1000 + count + 1}",
                CustomerName = dto.CustomerName.Trim(),
                Email = dto.Email.Trim(),
                Phone = dto.Phone.Trim(),
                CompanyName = dto.CompanyName?.Trim(),
                Address = dto.Address?.Trim(),
                City = dto.City?.Trim(),
                State = dto.State?.Trim(),
                Status = dto.Status ?? "Active",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = currentUserId,
                OwnerId = currentUserId
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                module: "API Customer",
                action: "Create",
                entityName: "Customer",
                recordId: customer.CustomerId.ToString(),
                result: "Success",
                details: $"Customer '{customer.CustomerName}' created via REST API."
            );

            var resultDto = new CustomerDto
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status,
                CreatedDate = customer.CreatedDate,
                OwnerId = customer.OwnerId
            };

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.CustomerId }, resultDto);
        }

        /// <summary>
        /// Update an existing customer
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CreateCustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var customer = await _crmService.GetScopedCustomers(User)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return NotFound(new { message = $"Customer with ID {id} not found or access denied." });
            }

            var (isDuplicate, dupReason) = await _crmService.CheckDuplicateCustomerAsync(
                dto.Email, dto.Phone, dto.CustomerName, dto.CompanyName, id);

            if (isDuplicate)
            {
                return Conflict(new { message = dupReason });
            }

            customer.CustomerName = dto.CustomerName.Trim();
            customer.Email = dto.Email.Trim();
            customer.Phone = dto.Phone.Trim();
            customer.CompanyName = dto.CompanyName?.Trim();
            customer.Address = dto.Address?.Trim();
            customer.City = dto.City?.Trim();
            customer.State = dto.State?.Trim();
            customer.Status = dto.Status ?? customer.Status;
            customer.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                module: "API Customer",
                action: "Update",
                entityName: "Customer",
                recordId: customer.CustomerId.ToString(),
                result: "Success",
                details: $"Customer '{customer.CustomerName}' updated via REST API."
            );

            return Ok(new { message = "Customer updated successfully." });
        }

        /// <summary>
        /// Delete customer by ID
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _crmService.GetScopedCustomers(User)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return NotFound(new { message = $"Customer with ID {id} not found or access denied." });
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                module: "API Customer",
                action: "Delete",
                entityName: "Customer",
                recordId: id.ToString(),
                result: "Success",
                details: $"Customer '{customer.CustomerName}' deleted via REST API."
            );

            return Ok(new { message = "Customer deleted successfully." });
        }
    }
}
