using Microsoft.AspNetCore.Mvc;

namespace FootballManagerBackend.Controllers
{
    [ApiController]
    [Route("/v1/testexampleapi")]
    public class ExampleController : ControllerBase
    {
        [HttpGet]
        public string Get() //һ��ʾ��API������Hello World����ӦGET�����������
        {
            return "Hello World!";
        }
    }
}
