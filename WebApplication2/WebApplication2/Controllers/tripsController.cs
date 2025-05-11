using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using WebApplication2.Exceptions;
using WebApplication2.Services;

namespace WebApplication2.Controllers;

[ApiController]
[Route("[Controller]")] 
public class tripsController(IDbService service) :ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllTrips()
    {
        return Ok(await service.GetTripsAsync());
    }

    

}