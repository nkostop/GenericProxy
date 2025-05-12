using Microsoft.AspNetCore.Mvc;

namespace Nbg.NetCore.Esign.Proxy.Controllers
{
    [ApiController]
    [Route("api/test")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("GET to Proxy is working");
        }

        [HttpPost]
        public IActionResult Post()
        {
            return Ok("POST to Proxy is working");
        }
    }
}