using Azure.Storage.Blobs;
using EnterpriseAI.Application.Interfaces;
using EnterpriseAI.Application.Services;
using EnterpriseAI.Infrastructure;
using EnterpriseAI.Infrastructure.AI;
using EnterpriseAI.Infrastructure.Configuration;
using EnterpriseAI.Infrastructure.Data;
using EnterpriseAI.Infrastructure.DocumentProcessing;
using EnterpriseAI.Infrastructure.Persistence;
using EnterpriseAI.Infrastructure.Repositories;
using EnterpriseAI.Infrastructure.Search;
using EnterpriseAI.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddSingleton<IDocumentChunkingService, DocumentChunkingService>();
builder.Services.AddSingleton<IEmbeddingService, AzureOpenAIEmbeddingService>();
builder.Services.AddScoped<IDocumentTextExtractor, DocumentTextExtractor>();
builder.Services.AddScoped<IDocumentIngestionService, DocumentIngestionService>();
builder.Services.AddScoped<IDocumentSearchIndexService, AzureDocumentSearchIndexService>();
builder.Services.AddScoped<IDocumentSearchIndexService, AzureDocumentSearchIndexService>();
builder.Services.AddScoped<IDocumentSearchService, AzureDocumentSearchService>();
builder.Services.AddScoped<IConversationHistoryService, AzureTableConversationHistoryService>();


builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FDE Knowledge Assistant API",
        Version = "v1",
        Description = "Enterprise AI Knowledge Assistant API"
    });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.Configure<AzureAISearchOptions>(builder.Configuration.GetSection("AzureAISearch"));


builder.Services.Configure<AzureTableStorageOptions>(builder.Configuration.GetSection("AzureTableStorage"));

var connectionString = builder.Configuration["AzureStorage:ConnectionString"];


if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Azure Storage connection string is missing.");
}

//var azureBlobOptions = new AzureBlobOptions
//{
//    ConnectionString =
//        builder.Configuration[
//            "AzureBlob:ConnectionString"]
//        ?? throw new InvalidOperationException(
//            "Azure Blob connection string is not configured."),

//    ContainerName =
//        builder.Configuration[
//            "AzureBlob:ContainerName"]
//        ?? "knowledge-documents"
//};

builder.Services.Configure<AzureStorageOptions>(
    builder.Configuration.GetSection("AzureStorage"));


builder.Services.AddSingleton(
    new BlobServiceClient(connectionString));

// Configure AI service options from configuration (always register options)
builder.Services.Configure<AzureOpenAIOptions>(
    builder.Configuration.GetSection("AzureOpenAI"));

var azureSection = builder.Configuration.GetSection("AzureOpenAI");
if (azureSection.Exists()
    && !string.IsNullOrWhiteSpace(azureSection["Endpoint"]) 
    && !string.IsNullOrWhiteSpace(azureSection["ApiKey"]) 
    && !string.IsNullOrWhiteSpace(azureSection["DeploymentName"]))
{
    // Register the real Azure OpenAI implementation when configuration is present
    builder.Services.AddScoped<IAiService, AzureOpenAIService>();
}
else
{
    // Use a simple mock AI service when Azure configuration is missing or invalid
    builder.Services.AddScoped<IAiService, MockAiService>();
}

builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSingleton(
//    azureBlobOptions);

builder.Services.AddScoped<
    IDocumentStorageService,
    AzureBlobDocumentService>();
var app = builder.Build();

// Swagger middleware
if (app.Environment.IsDevelopment())
{
    // Show detailed exceptions in development to help diagnose errors
    app.UseDeveloperExceptionPage();

    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FDE Knowledge Assistant API v1");
        options.RoutePrefix = "swagger";
    });
}


app.UseHttpsRedirection();

app.MapControllers();

// Configure the HTTP request pipeline.
// (Development-specific middlewares were already added above.)

app.Run();


