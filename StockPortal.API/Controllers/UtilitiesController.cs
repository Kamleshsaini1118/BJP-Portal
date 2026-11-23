using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockPortal.Core.Common;
using StockPortal.Core.DTOs.Utilities;
using StockPortal.Core.Interfaces;

namespace StockPortal.API.Controllers
{
    [ApiController]
    [Route("api/v1/utilities")]
    [Authorize]
    public class UtilitiesController : ControllerBase
    {
        private readonly IUtilityRepository _utilityRepo;

        public UtilitiesController(IUtilityRepository utilityRepo)
        {
            _utilityRepo = utilityRepo;
        }

        [HttpGet("expenses")]
        public async Task<ActionResult<ApiResponse<List<UtilityExpenseDto>>>> GetExpenses([FromQuery] string? searchQuery)
        {
            var expenses = await _utilityRepo.GetExpensesAsync(searchQuery);
            return Ok(ApiResponse<List<UtilityExpenseDto>>.Ok(expenses));
        }

        [HttpPost("expenses")]
        public async Task<ActionResult<ApiResponse<int>>> SaveExpense([FromBody] SaveUtilityExpenseDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Category) || string.IsNullOrWhiteSpace(dto.Location))
            {
                return BadRequest(ApiResponse<int>.Fail("Category and location are required."));
            }

            var id = await _utilityRepo.SaveExpenseAsync(dto);
            return Ok(ApiResponse<int>.Ok(id, "Utility expense saved successfully."));
        }

        [HttpDelete("expenses/{id:int}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteExpense(int id)
        {
            var success = await _utilityRepo.DeleteExpenseAsync(id);
            return Ok(ApiResponse<bool>.Ok(success, "Utility expense deleted successfully."));
        }

        [HttpGet("reminders")]
        public async Task<ActionResult<ApiResponse<List<UtilityReminderDto>>>> GetReminders([FromQuery] string? searchQuery)
        {
            var reminders = await _utilityRepo.GetRemindersAsync(searchQuery);
            return Ok(ApiResponse<List<UtilityReminderDto>>.Ok(reminders));
        }

        [HttpPost("reminders")]
        public async Task<ActionResult<ApiResponse<int>>> SaveReminder([FromBody] SaveUtilityReminderDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UtilityType))
            {
                return BadRequest(ApiResponse<int>.Fail("Utility type is required."));
            }

            var id = await _utilityRepo.SaveReminderAsync(dto);
            return Ok(ApiResponse<int>.Ok(id, "Reminder saved successfully."));
        }

        [HttpDelete("reminders/{id:int}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteReminder(int id)
        {
            var success = await _utilityRepo.DeleteReminderAsync(id);
            return Ok(ApiResponse<bool>.Ok(success, "Reminder deleted successfully."));
        }
    }
}
