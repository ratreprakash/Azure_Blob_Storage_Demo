using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure_Blob_Storage_Demo.Model;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Azure_Blob_Storage_Demo.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]   
    public class AzureContainerController : ControllerBase

    {

        readonly BlobServiceClient _blobServiceClient;
        readonly APIResponse _apiResponse;
        public AzureContainerController(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
            _apiResponse=new();
        }

        [HttpPost]
        public  async Task<ActionResult<APIResponse>> CreateContainerAsync([FromBody] BlobContainer blobContainer)
        {
                       

            try
            {
                string containerName = blobContainer.ContainerName;
                // Create the container
                BlobContainerClient container = await _blobServiceClient.CreateBlobContainerAsync(containerName);

                if (await container.ExistsAsync())
                {
                    _apiResponse.StatusCode=HttpStatusCode.OK;
                    _apiResponse.IsSuccess=true;
                    _apiResponse.Result=container;

                    return Ok(_apiResponse);
                }
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
        public async  Task<ActionResult<APIResponse>> ListContainers(string? prefix, int? segmentSize)
        {
            List<string> blobContainers = new List<string>();
            try
            {

               
                // Call the listing operation and enumerate the result segment.
                var resultSegment = _blobServiceClient.GetBlobContainersAsync(BlobContainerTraits.Metadata, prefix, default)
                    .AsPages(default, segmentSize);

                await foreach (Azure.Page<BlobContainerItem> containerPage in resultSegment)
                {
                    foreach (BlobContainerItem containerItem in containerPage.Values)
                    {
                        blobContainers.Add(containerItem.Name);                     

                    }                  
                }

                _apiResponse.StatusCode=HttpStatusCode.OK;
                _apiResponse.IsSuccess=true;
                _apiResponse.Result=blobContainers;

                return Ok(_apiResponse);


            }
            catch (RequestFailedException e)
            {
                _apiResponse.IsSuccess = false;
                _apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                _apiResponse.Message = new List<string> { e.Message };
            }
            return  _apiResponse;


        }

        [HttpPost]
        public async Task<ActionResult<APIResponse>> DeleteContainer([FromBody] BlobContainer blobContainer)
        {

            try
            {
                BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(blobContainer.ContainerName);

                _apiResponse.Result= await containerClient.DeleteAsync();
                _apiResponse.IsSuccess = true;
                _apiResponse.Message= new List<string> { "Contianer Delete Suceessfully" };
                _apiResponse.StatusCode= HttpStatusCode.OK;
                return Ok(_apiResponse);



            }
            catch (RequestFailedException ex)
            {
                _apiResponse.IsSuccess= false;
                _apiResponse.Message = new List<string> { ex.Message };
                _apiResponse.StatusCode = HttpStatusCode.InternalServerError;

            }

            return _apiResponse;

        }


        [HttpPost]
        public async Task<ActionResult<APIResponse>> RestoreContainer([FromBody] BlobContainer container)
        {
            try
            {
                await foreach (BlobContainerItem item in _blobServiceClient.GetBlobContainersAsync(BlobContainerTraits.None, BlobContainerStates.Deleted))
                {
                    if (item.Name == container.ContainerName && (item.IsDeleted == true))
                    {
                        try
                        {
                            _apiResponse.Result=  await _blobServiceClient.UndeleteBlobContainerAsync(container.ContainerName, item.VersionId);

                            _apiResponse.IsSuccess= true;
                            _apiResponse.Message = new List<string> { "Container Successfull Restored" };
                            _apiResponse.StatusCode = HttpStatusCode.OK;

                            return Ok(_apiResponse);
                        }
                        catch (RequestFailedException e)
                        {
                            _apiResponse.IsSuccess= false;
                            _apiResponse.Message= new List<string> { e.Message };
                            _apiResponse.StatusCode = HttpStatusCode.InternalServerError;

                        }
                    }
                }
            }
            catch (Exception e)
            {

                _apiResponse.IsSuccess= false;
                _apiResponse.Message= new List<string> { e.Message };
                _apiResponse.StatusCode = HttpStatusCode.InternalServerError;
            }


            return _apiResponse;
        }
    }
}
