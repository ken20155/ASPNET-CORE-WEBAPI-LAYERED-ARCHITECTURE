using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApiInterviewStatus.Models;

namespace WebApiInterviewStatus.Controllers.V1
{

    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class SampleLangController(MainModel mainModel) : ControllerBase
    {
        private readonly MainModel _mainModel = mainModel;


        [HttpGet("health-status")]
        public async Task<IActionResult> Health()
        {
            var sql = "SELECT * FROM [tbl_users] WHERE is_deleted = @is_deleted ";

            var allRows = await _mainModel.GetAllAsync(
                sql,
                new { is_deleted = 0 },
                dbSet: DbSetType.Db2
            );

            return Ok(new
            {
                success = true,
                message = "Fetched data21",
                data = allRows
            });
        }
    }
}
