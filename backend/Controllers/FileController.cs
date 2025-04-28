using System;
using System.Threading.Tasks;
using backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly ILogger<FileController> _logger;
        private readonly IChatService _chatService;

        public FileController(ILogger<FileController> logger, IChatService chatService)
        {
            _logger = logger;
            _chatService = chatService;
        }

        /// <summary>
        /// 上传文件接口
        /// </summary>
        /// <param name="file">上传的文件</param>
        /// <returns>文件URL及相关信息</returns>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")] // 明确指定接受的媒体类型
        [RequestSizeLimit(100 * 1024 * 1024)] // 限制100MB
        [RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)] // 限制100MB
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("上传的文件为空");
                    return BadRequest("没有选择文件或文件为空");
                }

                _logger.LogInformation($"接收到文件上传请求: {file.FileName}, 大小: {file.Length} 字节");

                // 调用服务上传文件
                var (fileUrl, fileName, fileSize) = await _chatService.UploadTempFileAsync(file);
                
                _logger.LogInformation($"文件上传成功: {fileName}, URL: {fileUrl}");
                
                // 返回文件信息
                return Ok(new { 
                    url = fileUrl, 
                    fileName = fileName,
                    fileSize = fileSize,
                    isImage = IsImageFile(fileName)
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "文件上传参数错误");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文件上传异常");
                return StatusCode(500, "文件上传失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 判断文件是否为图片
        /// </summary>
        private bool IsImageFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;
                
            string ext = System.IO.Path.GetExtension(fileName).ToLower();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif" || ext == ".bmp" || ext == ".webp";
        }
    }
} 