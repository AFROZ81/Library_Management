using LibraryPro.Web.Models.Api;
using LibraryPro.Web.Models.Entities;
using LibraryPro.Web.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace LibraryPro.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class MembersApiController : ControllerBase
{
    private readonly IMemberRepository _memberRepository;
    private readonly ILogger<MembersApiController> _logger;

    public MembersApiController(IMemberRepository memberRepository, ILogger<MembersApiController> logger)
    {
        _memberRepository = memberRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all members
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApiMemberDto>>> GetMembers()
    {
        try
        {
            var members = await _memberRepository.GetAllAsync();
            var memberDtos = members.Select(m => new ApiMemberDto
            {
                Id = m.Id,
                Name = m.Name ?? string.Empty,
                Email = m.Email ?? string.Empty,
                PhoneNumber = m.PhoneNumber ?? string.Empty,
                MembershipDate = m.MembershipDate,
                ReceiveDueDateReminders = m.ReceiveDueDateReminders,
                ReceiveOverdueNotices = m.ReceiveOverdueNotices,
                ReceiveReservationAlerts = m.ReceiveReservationAlerts,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            return Ok(memberDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting members");
            return StatusCode(500, new { error = "An error occurred while retrieving members" });
        }
    }

    /// <summary>
    /// Get a member by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiMemberDto>> GetMember(int id)
    {
        try
        {
            var member = await _memberRepository.GetByIdAsync(id);
            if (member == null)
            {
                return NotFound(new { error = "Member not found" });
            }

            var memberDto = new ApiMemberDto
            {
                Id = member.Id,
                Name = member.Name ?? string.Empty,
                Email = member.Email ?? string.Empty,
                PhoneNumber = member.PhoneNumber ?? string.Empty,
                MembershipDate = member.MembershipDate,
                ReceiveDueDateReminders = member.ReceiveDueDateReminders,
                ReceiveOverdueNotices = member.ReceiveOverdueNotices,
                ReceiveReservationAlerts = member.ReceiveReservationAlerts,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            return Ok(memberDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting member with ID {MemberId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the member" });
        }
    }

    /// <summary>
    /// Create a new member
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiMemberDto>> CreateMember([FromBody] CreateMemberDto createMemberDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var member = new Member
            {
                Name = createMemberDto.Name,
                Email = createMemberDto.Email,
                PhoneNumber = createMemberDto.PhoneNumber,
                MembershipDate = DateTime.UtcNow,
                ReceiveDueDateReminders = createMemberDto.ReceiveDueDateReminders,
                ReceiveOverdueNotices = createMemberDto.ReceiveOverdueNotices,
                ReceiveReservationAlerts = createMemberDto.ReceiveReservationAlerts
            };

            await _memberRepository.AddAsync(member);

            var memberDto = new ApiMemberDto
            {
                Id = member.Id,
                Name = member.Name ?? string.Empty,
                Email = member.Email ?? string.Empty,
                PhoneNumber = member.PhoneNumber ?? string.Empty,
                MembershipDate = member.MembershipDate,
                ReceiveDueDateReminders = member.ReceiveDueDateReminders,
                ReceiveOverdueNotices = member.ReceiveOverdueNotices,
                ReceiveReservationAlerts = member.ReceiveReservationAlerts,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return CreatedAtAction(nameof(GetMember), new { id = member.Id }, memberDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating member");
            return StatusCode(500, new { error = "An error occurred while creating the member" });
        }
    }

    /// <summary>
    /// Update an existing member
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiMemberDto>> UpdateMember(int id, [FromBody] UpdateMemberDto updateMemberDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var member = await _memberRepository.GetByIdAsync(id);
            if (member == null)
            {
                return NotFound(new { error = "Member not found" });
            }

            member.Name = updateMemberDto.Name;
            member.PhoneNumber = updateMemberDto.PhoneNumber;
            member.ReceiveDueDateReminders = updateMemberDto.ReceiveDueDateReminders;
            member.ReceiveOverdueNotices = updateMemberDto.ReceiveOverdueNotices;
            member.ReceiveReservationAlerts = updateMemberDto.ReceiveReservationAlerts;

            await _memberRepository.UpdateAsync(member);

            var memberDto = new ApiMemberDto
            {
                Id = member.Id,
                Name = member.Name ?? string.Empty,
                Email = member.Email ?? string.Empty,
                PhoneNumber = member.PhoneNumber ?? string.Empty,
                MembershipDate = member.MembershipDate,
                ReceiveDueDateReminders = member.ReceiveDueDateReminders,
                ReceiveOverdueNotices = member.ReceiveOverdueNotices,
                ReceiveReservationAlerts = member.ReceiveReservationAlerts,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return Ok(memberDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating member with ID {MemberId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the member" });
        }
    }

    /// <summary>
    /// Delete a member
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteMember(int id)
    {
        try
        {
            var member = await _memberRepository.GetByIdAsync(id);
            if (member == null)
            {
                return NotFound(new { error = "Member not found" });
            }

            await _memberRepository.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting member with ID {MemberId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the member" });
        }
    }
}
