using Microsoft.AspNetCore.Mvc;
using WebApplication2.Exceptions;
using WebApplication2.Models.DTOs;
using WebApplication2.Services;

namespace WebApplication2.Controllers;

[ApiController]
[Route("[Controller]")]
public class clientsController(IDbService service) :ControllerBase
{
    [HttpGet]
    [Route("{id}/trips")]
    public async Task<IActionResult> GetAllClientTripsByClientId(int id)
    {
        return Ok(await service.GetAllTripsByClientIdAsync(id));
    }
    

    [HttpPut]
    [Route("{clientId}/trips/{tripId}")]
    public async Task<IActionResult> reservationToTrip([FromRoute] int clientId, [FromRoute]int tripId )
    {
        try
        {
            await service.reservationToTripDB(clientId, tripId);
            return NoContent();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch(BadRequestException e)
        {
            return BadRequest(e.Message);
        }

    }

    [HttpDelete]
    [Route("{clientId}/trips/{tripId}")]
    public async Task<IActionResult> deleteSign([FromRoute] int clientId, [FromRoute] int tripId)
    {
        try
        {
            await service.deleteReservationDB(clientId, tripId);
            return NoContent();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }



    }

    [HttpPost]
    public async Task<IActionResult> AddClient(
        [FromBody] ClientGetDTO cgd)
    {
        var newClientId = await service.AddClientDB(cgd);
        return Ok(newClientId.Id);
    }
}