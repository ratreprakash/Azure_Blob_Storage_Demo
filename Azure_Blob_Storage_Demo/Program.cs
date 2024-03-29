using Azure.Identity;
using Azure_Blob_Storage_Demo.Filter;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option=>
{
    option.OperationFilter<FileUploadFilter>();
    
});
builder.Services.AddAzureClients(option =>
{

    option.AddBlobServiceClient(builder.Configuration.GetConnectionString("AzureStorage"));
    option.UseCredential(new DefaultAzureCredential());
    
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    
    options.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
