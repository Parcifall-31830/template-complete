using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using WebApplication2.Exceptions;
using WebApplication2.Models;
using WebApplication2.Models.DTOs;

namespace WebApplication2.Services;

public interface  IDbService
{
    public Task<IEnumerable<TripGetDTO>> GetTripsAsync();
    public Task<IEnumerable<ClientTripWithInfoDTO>>GetAllTripsByClientIdAsync(int id);
    public Task<Client> AddClientDB(ClientGetDTO cgd);
    public Task reservationToTripDB(int clientId,int tripId);
    public Task deleteReservationDB(int clientId, int tripId);

}

public class DbService(IConfiguration config): IDbService
{
    public async Task<IEnumerable<TripGetDTO>> GetTripsAsync()
    {
        var result = new List<TripGetDTO>();
        var connectionString = config.GetConnectionString("Default");

        await using var connection = new SqlConnection(connectionString);
        
        var sql= @"Select t.IdTrip,t.Name,t.Description,t.DateFrom,t.DateTo,t.MaxPeople,c.Name from Trip t
           inner join Country_Trip ct on t.IdTrip = ct.IdTrip
           inner join Country c on ct.IdCountry = c.IdCountry
        ";
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync();
        
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new TripGetDTO
            {
                IdTrip = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = reader.GetInt32(5),
                Country = reader.GetString(6),  
            });
        }
        
        return result;
        
    }

    public async Task<IEnumerable<ClientTripWithInfoDTO>> GetAllTripsByClientIdAsync(int clientId)
    {
        var result = new List<ClientTripWithInfoDTO>();
        var connectionString = config.GetConnectionString("Default");

        await using var connection = new SqlConnection(connectionString);
        
        var checkClientSql = "SELECT COUNT(1) FROM Client WHERE IdClient = @ClientId";
        await using (var checkCmd = new SqlCommand(checkClientSql, connection))
        {
            checkCmd.Parameters.AddWithValue("@ClientId", clientId);
            await connection.OpenAsync();
            var exists = (int)await checkCmd.ExecuteScalarAsync() > 0;
            if (!exists)
            {
                throw new NotFoundException($"Client with ID {clientId} does not exist.");
            }
            await connection.CloseAsync();
        }
        
        var sql = @"
        SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
               ct.RegisteredAt, ct.PaymentDate
        FROM Client_Trip ct
        JOIN Trip t ON t.IdTrip = ct.IdTrip
        WHERE ct.IdClient = @ClientId";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ClientId", clientId);
        await connection.OpenAsync();

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new ClientTripWithInfoDTO()
            {
                IdTrip = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = reader.GetInt32(5),
                RegisteredAt = reader.GetInt32(6),
                PaymentDate = reader.IsDBNull(7) ? null : reader.GetInt32(7)
            });
        }

        return result;
    }

    public async Task<Client> AddClientDB(ClientGetDTO client)
    {
        if (
            string.IsNullOrWhiteSpace(client.FirstName) ||
            string.IsNullOrWhiteSpace(client.LastName) ||
            string.IsNullOrWhiteSpace(client.Email) ||
            string.IsNullOrWhiteSpace(client.Telephone) ||
            string.IsNullOrWhiteSpace(client.Pesel)
        )
        {
            throw new BadRequestException("Please provide valid client data.");
        }

        if (client.Pesel.Length != 11)
        {
            throw new BadRequestException("Pesel must be 11 characters long.");
        }

        var connectionString = config.GetConnectionString("Default");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        var getIdSql = "SELECT MAX(IdClient) + 1 FROM Client";
        await using var getIdCmd = new SqlCommand(getIdSql, connection);
        var newId = (int)await getIdCmd.ExecuteScalarAsync();

        
        var sql = @"INSERT INTO Client(FirstName, LastName, Email, Telephone, Pesel) 
                VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@FirstName", client.FirstName);
        command.Parameters.AddWithValue("@LastName", client.LastName);
        command.Parameters.AddWithValue("@Email", client.Email);
        command.Parameters.AddWithValue("@Telephone", client.Telephone);
        command.Parameters.AddWithValue("@Pesel", client.Pesel);

        await command.ExecuteNonQueryAsync();

        return new Client()
        {
            Id = newId,
            FirstName = client.FirstName,
            LastName = client.LastName,
            Email = client.Email,
            Telephone = client.Telephone,
            Pesel = client.Pesel,
        };
    }

    public async Task reservationToTripDB(int clientId,int tripId)
    {
        var connectionString = config.GetConnectionString("Default");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        var checkClientSql = "SELECT COUNT(1) FROM Client WHERE IdClient = @clientId";
        var checkTripSql = "SELECT COUNT(1) FROM Trip WHERE IdTrip = @tripId";
        var checkClientTripSql = "SELECT COUNT(1) FROM Client_Trip WHERE IdTrip = @tripId AND IdClient = @ClientId";

        await using (var checkCmdClient = new SqlCommand(checkClientSql, connection))
        {
            checkCmdClient.Parameters.AddWithValue("@clientId",clientId );
            var clientExist = (int)await checkCmdClient.ExecuteScalarAsync()>0;
            if (!clientExist)
            {
                throw new NotFoundException($"Client with ID {clientId} does not exist.");
            }
        }

        await using (var checkCmdTrip = new SqlCommand(checkTripSql, connection))
        {
            checkCmdTrip.Parameters.AddWithValue("@tripId", tripId);
            var tripExist = (int)await checkCmdTrip.ExecuteScalarAsync()>0;
            if (!tripExist)
            {
                throw new NotFoundException($"Trip with ID {tripId} does not exist.");
            }
        }

        await using (var checkCmdTrip = new SqlCommand(checkClientTripSql, connection))
        {
            checkCmdTrip.Parameters.AddWithValue("@clientId", clientId);
            checkCmdTrip.Parameters.AddWithValue("@tripId", tripId);
            var tripExist = (int)await checkCmdTrip.ExecuteScalarAsync()>0;
            if (tripExist)
            {
                throw new BadRequestException($"Connection between Client with ID {clientId} and Trip with ID {tripId} already exists.");
            }
        }
        
        var sql = @"Insert into Client_Trip(IdClient,IdTrip,RegisteredAt) 
                    values(@clientId,@tripId,@date)";
        
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@tripId", tripId);
        command.Parameters.AddWithValue("@date", int.Parse("" + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day));

        await command.ExecuteNonQueryAsync();
    
    
       
        
    }

    public async Task deleteReservationDB(int clientId, int tripId)
    {
        var connetionString = config.GetConnectionString("Default");
        await using var connection = new SqlConnection(connetionString);
        await connection.OpenAsync();
        
        var checkSql = "SELECT COUNT(1) FROM Client_Trip WHERE IdClient = @clientId and IdTrip = @tripId";
        await using (var checkExist = new SqlCommand(checkSql, connection))
        {
            checkExist.Parameters.AddWithValue("@clientId", clientId);
            checkExist.Parameters.AddWithValue("@tripId", tripId);
            var exist =(int)await checkExist.ExecuteScalarAsync()>0;
            if (!exist)
            {
                throw new NotFoundException($"Connection between Client with ID {clientId} and Trip with ID {tripId} does not exist.");
            }
        }
        
        var sql = @"Delete from Client_Trip where IdClient = @clientId and IdTrip = @tripId";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@tripId", tripId);
        await command.ExecuteNonQueryAsync();


    }





}