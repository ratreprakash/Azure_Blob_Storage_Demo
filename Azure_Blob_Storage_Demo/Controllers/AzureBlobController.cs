using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure_Blob_Storage_Demo.Model;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Azure_Blob_Storage_Demo.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AzureBlobController : ControllerBase
    {


        readonly BlobServiceClient _blobServiceClient;
        readonly APIResponse _apiResponse;
        public AzureBlobController(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
            _apiResponse=new();

        }


        public static async Task<byte[]> GetBytesFromIFormFile(IFormFile formFile)
        {
            if (formFile == null || formFile.Length == 0)
            {
                return null; // Handle empty files or null input gracefully
            }

            using (var memoryStream = new MemoryStream())
            {
                await formFile.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }



        [HttpPost]
        
        public async Task<ActionResult<APIResponse>> UploadBlob( IFormFile file)
        {


            try
            {

                // Create the container
                BlobContainerClient container = _blobServiceClient.GetBlobContainerClient("testmart");


                if (file == null || file.Length == 0)
                {
                    return null; // Handle empty files or null input gracefully
                }
                BlobClient blobClient = container.GetBlobClient(file.FileName);

                byte[] fileBytes = await GetBytesFromIFormFile(file);
                BinaryData binaryData = new BinaryData(fileBytes);
                _apiResponse.Result= await blobClient.UploadAsync(binaryData, true);
                //FileStream fileStream = GetBytesFromIFormFile()
                _apiResponse.StatusCode=HttpStatusCode.OK;
                _apiResponse.IsSuccess=true;
                _apiResponse.Result=container;

                return Ok(_apiResponse);
            }
            catch (RequestFailedException e)
            {
                _apiResponse.IsSuccess = false;
                _apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                _apiResponse.Message = new List<string> { e.Message };
            }

            return _apiResponse;
        }


        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetBlobsList(string? prefix, int? segmentSize,string containerName)
        {


            try
            {
                List<string> Bloblist = new List<string>();
                // Create the container
                BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(containerName);


                var resultSegment = container.GetBlobsAsync().AsPages(default, segmentSize);

                // Enumerate the blobs returned for each page.
                await foreach (Page<BlobItem> blobPage in resultSegment)
                {
                    foreach (BlobItem blobItem in blobPage.Values)
                    {
                        Bloblist.Add(blobItem.Name);
                        
                    }                  
                }

                _apiResponse.Result = Bloblist;               
                _apiResponse.StatusCode=HttpStatusCode.OK;
                _apiResponse.IsSuccess=true;
                

                return Ok(_apiResponse);
            }
            catch (RequestFailedException e)
            {
                _apiResponse.IsSuccess = false;
                _apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                _apiResponse.Message = new List<string> { e.Message };
            }

            return _apiResponse;
        }



        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetDeletedBlobsList(string? prefix, int? segmentSize, string containerName)
        {


            try
            {
                List<string> Bloblist = new List<string>();
                // Create the container
                BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(containerName);


                var resultSegment = container.GetBlobsAsync(BlobTraits.None,BlobStates.Deleted).AsPages(default, segmentSize);

                // Enumerate the blobs returned for each page.
                await foreach (Page<BlobItem> blobPage in resultSegment)
                {
                    foreach (BlobItem blobItem in blobPage.Values)
                    {
                        if(blobItem.Deleted==true)
                        {
                            Bloblist.Add(blobItem.Name);
                        }
                       

                    }
                }

                _apiResponse.Result = Bloblist;
                _apiResponse.StatusCode=HttpStatusCode.OK;
                _apiResponse.IsSuccess=true;


                return Ok(_apiResponse);
            }
            catch (RequestFailedException e)
            {
                _apiResponse.IsSuccess = false;
                _apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                _apiResponse.Message = new List<string> { e.Message };
            }

            return _apiResponse;
        }





        [HttpPost]
        public async Task<ActionResult<APIResponse>> DownloadBlob(string containerName,string blobName)
        {


            try
            {

                // Create the container
                BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(containerName);
               
                BlockBlobClient blobClient2 = container.GetBlockBlobClient(blobName);

                BlobProperties properties = await blobClient2.GetPropertiesAsync();

                Stream blobStream = await blobClient2.OpenReadAsync();
               
             
                return File(blobStream,properties.ContentType, blobClient2.Name);
                
            }
            catch (RequestFailedException e)
            {
                _apiResponse.IsSuccess = false;
                _apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                _apiResponse.Message = new List<string> { e.Message };
            }

            return _apiResponse;
        }



        [HttpPost]
        public async Task<ActionResult<APIResponse>> DeleteBlob(string containerName, string blobName)
        {
            try
            {                // Create the container
                BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(containerName);
                BlockBlobClient blobClient2 = container.GetBlockBlobClient(blobName);
                _apiResponse.Result = await blobClient2.DeleteIfExistsAsync();
                _apiResponse.StatusCode=HttpStatusCode.OK;
                _apiResponse.IsSuccess=true;

            }
            catch (RequestFailedException e)
            {
                _apiResponse.IsSuccess = false;
                _apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                _apiResponse.Message = new List<string> { e.Message };
            }

            return _apiResponse;
        }


        [HttpPost]
        public async Task<ActionResult<APIResponse>> RestoreBlob(string containerName, string blobName)
        {
            try
            {                
                BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(containerName);


                //Pageable<BlobItem> blob = container.GetBlobs(BlobTraits.None, BlobStates.Deleted);
                //Pageable<BlobItem>  blobClient2 = container.GetBlobs(BlobStates.Deleted);
                foreach (BlobItem blob in container.GetBlobs(BlobTraits.None, BlobStates.Deleted))
                {
                    if (blob.Name==blobName)
                        await container.GetBlockBlobClient(blob.Name).UndeleteAsync();
                }
                _apiResponse.Result=null;
                _apiResponse.Message = new List<string> { $"{blobName} is successfully retsore" };

                _apiResponse.StatusCode=HttpStatusCode.OK;
                _apiResponse.IsSuccess=true;

            }
            catch (RequestFailedException e)
            {
                _apiResponse.IsSuccess = false;
                _apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                _apiResponse.Message = new List<string> { e.Message };
            }

            return _apiResponse;
        }



    }
}
