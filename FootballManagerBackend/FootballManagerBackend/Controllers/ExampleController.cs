using Microsoft.AspNetCore.Mvc;

namespace FootballManagerBackend.Controllers
{
    [ApiController]
    [Route("/v1/testexampleapi")]
    public class ExampleController : ControllerBase
    {
        [HttpGet]
        public string Get() //一个示例API，返回Hello World，响应GET请求，无需参数
        {
            return "Hello World!";
        }
    }
}
