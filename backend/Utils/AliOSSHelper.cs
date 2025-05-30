using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Aliyun.OSS;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace backend.Utils
{
    /// <summary>
    /// 阿里云OSS帮助类
    /// </summary>
    public class AliOSSHelper
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AliOSSHelper> _logger;
        private readonly string _endpoint;
        private readonly string _accessKeyId;
        private readonly string _accessKeySecret;
        private readonly string _bucketName;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configuration">配置信息</param>
        /// <param name="logger">日志</param>
        public AliOSSHelper(IConfiguration configuration, ILogger<AliOSSHelper> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            // 从配置中获取阿里云OSS配置
            _endpoint = _configuration["AliOSS:Endpoint"];
            _accessKeyId = _configuration["AliOSS:AccessKeyId"];
            _accessKeySecret = _configuration["AliOSS:AccessKeySecret"];
            _bucketName = _configuration["AliOSS:BucketName"];
            
            if (string.IsNullOrEmpty(_endpoint) || string.IsNullOrEmpty(_accessKeyId) || 
                string.IsNullOrEmpty(_accessKeySecret) || string.IsNullOrEmpty(_bucketName))
            {
                _logger.LogError("阿里云OSS配置不完整，请检查appsettings.json");
                throw new ArgumentException("阿里云OSS配置不完整");
            }
        }

        /// <summary>
        /// 上传文件到OSS
        /// </summary>
        /// <param name="file">文件对象</param>
        /// <param name="objectName">存储对象名称</param>
        /// <returns>文件访问URL</returns>
        public async Task<string> UploadFileAsync(IFormFile file, string objectName)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("文件为空");
            }
            
            // 创建临时文件
            string tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");
            
            try
            {
                // 保存文件到临时目录
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                
                // 使用HttpClient实现上传
                using (var httpClient = new HttpClient())
                {
                    // 读取文件
                    var fileBytes = await File.ReadAllBytesAsync(tempFilePath);
                    
                    // 创建请求内容
                    using (var content = new ByteArrayContent(fileBytes))
                    {
                        // 设置内容类型
                        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                        
                        // 构建访问URL
                        string uploadUrl = $"https://{_bucketName}.{_endpoint}/{objectName}";
                        
                        // 添加身份验证头信息
                        string dateString = DateTime.UtcNow.ToString("R");
                        string contentMd5 = "";
                        
                        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Date", dateString);
                        
                        // 构建签名字符串
                        string stringToSign = $"PUT\n{contentMd5}\n{file.ContentType}\n{dateString}\n/{_bucketName}/{objectName}";
                        
                        // 计算签名
                        using (var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_accessKeySecret)))
                        {
                            byte[] signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
                            string signature = Convert.ToBase64String(signatureBytes);
                            
                            // 添加授权头
                            string authorization = $"OSS {_accessKeyId}:{signature}";
                            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorization);
                        }
                        
                        // 发送PUT请求
                        var response = await httpClient.PutAsync(uploadUrl, content);
                        
                        // 检查响应
                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation($"文件 {file.FileName} 上传成功，对象名: {objectName}");
                            return $"https://{_bucketName}.{_endpoint}/{objectName}";
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            _logger.LogError($"上传文件失败，状态码: {response.StatusCode}，错误信息: {errorContent}");
                            throw new Exception($"上传失败，状态码: {response.StatusCode}，错误信息: {errorContent}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"上传文件到OSS失败: {ex.Message}");
                throw;
            }
            finally
            {
                // 清理临时文件
                if (File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"删除临时文件失败: {tempFilePath}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 删除OSS文件
        /// </summary>
        /// <param name="objectName">存储对象名称</param>
        /// <returns>是否删除成功</returns>
        public bool DeleteFile(string objectName)
        {
            try
            {
                // 创建OSSClient实例
                var client = new OssClient(_endpoint, _accessKeyId, _accessKeySecret);
                
                // 删除文件
                client.DeleteObject(_bucketName, objectName);
                
                _logger.LogInformation($"文件 {objectName} 删除成功");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除OSS文件失败: {ex.Message}");
                return false;
            }
        }
    }
}
