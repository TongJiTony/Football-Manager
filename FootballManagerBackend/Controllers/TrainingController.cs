using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using System.Text.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FootballManagerBackend.Controllers
{
    [ApiController]
    [Route("v1/training")]
    public class TrainingController : ControllerBase
    {
        private readonly OracleDbContext _context;

        public TrainingController(OracleDbContext context)
        {
            _context = context;
        }

        [HttpGet("displayall")] // GET /v1/training/displayall or GET /v1/training/displayall?teamid=* or GET /v1/training/displayall?stadiumid=*
        public async Task<IActionResult> Get(int? teamid = null, int? stadiumid = null)
        {
            string query = "SELECT training_id, train_focus, team_formation, train_score, team_familiarity, train_intension, train_stadium_id, train_team_id, teams.team_name AS train_team_name FROM trainings left outer join teams ON trainings.train_team_id = teams.team_id ORDER BY training_id";
            if (teamid != null)
            {
                query = "SELECT training_id, train_focus, team_formation, train_score, team_familiarity, train_intension, train_stadium_id, train_team_id, teams.team_name AS train_team_name FROM trainings left outer join teams ON trainings.train_team_id = teams.team_id WHERE train_team_id = :teamid ORDER BY training_id";
                var parameters = new Dictionary<string, object> { { "teamid", teamid } };
                List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
                return Ok(result);
            }
            else if (stadiumid != null)
            {
                query = "SELECT training_id, train_focus, team_formation, train_score, team_familiarity, train_intension, train_stadium_id, train_team_id, teams.team_name AS train_team_name FROM trainings left outer join teams ON trainings.train_team_id = teams.team_id WHERE train_stadium_id = :stadiumid ORDER BY training_id";
                var parameters = new Dictionary<string, object> { { "stadiumid", stadiumid } };
                List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
                return Ok(result);
            }
            else
            {
                List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query);
                return Ok(result);
            }
        }

        [HttpGet("displayone")] // GET /v1/training/displayone?trainingid=*
        public async Task<IActionResult> Get(int trainingid)
        {
            string query = "SELECT training_id, train_focus, team_formation, train_score, team_familiarity, train_intension, train_stadium_id, train_team_id, teams.team_name AS train_team_name FROM trainings left outer join teams ON trainings.train_team_id = teams.team_id WHERE training_id = :trainingid";
            var parameters = new Dictionary<string, object> { { "trainingid", trainingid } };
            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            return Ok(result);
        }

        [HttpPost("add")] // POST /v1/training/add + JSON
        public async Task<IActionResult> Add([FromBody] JsonElement trainingElement)
        {
            string query = @"
            INSERT INTO trainings 
            (training_id, train_focus, team_formation, train_score, team_familiarity, train_intension, train_stadium_id, train_team_id) 
            VALUES 
            (TRAINING_SEQ.NEXTVAL, :train_focus, :team_formation, :train_score, :team_familiarity, :train_intension, :train_stadium_id, :train_team_id) 
            RETURNING training_id INTO :new_id";

            var parameters = new Dictionary<string, object>();
            var outParameter = new OracleParameter("new_id", OracleDbType.Decimal, ParameterDirection.Output);

            foreach (var property in trainingElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "train_focus":
                        parameters.Add("train_focus", property.Value.GetString());
                        break;
                    case "team_formation":
                        parameters.Add("team_formation", property.Value.GetInt32());
                        break;
                    case "train_score":
                        parameters.Add("train_score", property.Value.GetInt32());
                        break;
                    case "team_familiarity":
                        parameters.Add("team_familiarity", property.Value.GetInt32());
                        break;
                    case "train_intension":
                        parameters.Add("train_intension", property.Value.GetString());
                        break;
                    case "train_stadium_id":
                        parameters.Add("train_stadium_id", property.Value.GetInt32());
                        break;
                    case "train_team_id":
                        parameters.Add("train_team_id", property.Value.GetInt32());
                        break;
                    default:
                        break;
                }
            }

            await _context.ExecuteNonQueryAsyncForAdd(query, parameters, outParameter);
            int newTrainingId = Convert.ToInt32(((OracleDecimal)outParameter.Value).Value);

            return CreatedAtAction(nameof(Get), new { TRAINING_ID = newTrainingId }, new { TRAINING_ID = newTrainingId });
        }

        [HttpPost("update")] // POST /v1/training/update?trainingid=* + JSON
        public async Task<IActionResult> Update(int trainingid, [FromBody] JsonElement trainingElement)
        {
            if (!trainingElement.EnumerateObject().Any())
            {
                return BadRequest("No fields provided for update.");
            }

            var queryBuilder = new System.Text.StringBuilder("UPDATE trainings SET ");
            var parameters = new Dictionary<string, object>();

            foreach (var property in trainingElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "training_id":
                        if (int.TryParse(property.Value.GetString(), out int training_id))
                        {
                            queryBuilder.Append("training_id = :training_id, ");
                            parameters.Add("training_id", training_id);
                        }
                        else
                        {
                            Console.WriteLine($"Invalid format for training_id: {property.Value.GetString()}");
                            return BadRequest("Invalid format for training_id.");
                        }
                        break;
                    case "train_focus":
                        queryBuilder.Append("train_focus = :train_focus, ");
                        parameters.Add("train_focus", property.Value.GetString());
                        break;
                    case "team_formation":
                        queryBuilder.Append("team_formation = :team_formation, ");
                        parameters.Add("team_formation", property.Value.GetInt32());
                        break;
                    case "train_score":
                        queryBuilder.Append("train_score = :train_score, ");
                        parameters.Add("train_score", property.Value.GetInt32());
                        break;
                    case "team_familiarity":
                        queryBuilder.Append("team_familiarity = :team_familiarity, ");
                        parameters.Add("team_familiarity", property.Value.GetInt32());
                        break;
                    case "team_intension":
                        queryBuilder.Append("team_intension = :team_intension, ");
                        parameters.Add("team_intension", property.Value.GetString());
                        break;
                    case "train_stadium_id":
                        queryBuilder.Append("train_stadium_id = :train_stadium_id, ");
                        parameters.Add("train_stadium_id", property.Value.GetInt32());
                        break;
                    case "train_team_id":
                        queryBuilder.Append("train_team_id = :train_team_id, ");
                        parameters.Add("train_team_id", property.Value.GetInt32());
                        break;
                    default:
                        break;
                }
            }

            queryBuilder.Length -= 2;
            queryBuilder.Append(" WHERE training_id = :trainingid");
            parameters.Add("trainingid", trainingid);
            string query = queryBuilder.ToString();
            Console.WriteLine($"Generated Query: {query}");

            try
            {
                await _context.ExecuteNonQueryAsync(query, parameters);
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing POST request: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("delete")] // DELETE /v1/training/delete?trainingid=*
        public async Task<IActionResult> Delete(int trainingid)
        {
            string query = "DELETE FROM trainings WHERE training_id = :trainingid";
            var parameters = new Dictionary<string, object> { { "trainingid", trainingid } };

            try
            {
                await _context.ExecuteNonQueryAsync(query, parameters);
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing DELETE request: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
