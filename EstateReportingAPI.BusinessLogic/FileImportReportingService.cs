using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using Microsoft.EntityFrameworkCore;
using SimpleResults;
using TransactionProcessor.Database.Contexts;
using Shared.EntityFramework;
using Shared.Results;

namespace EstateReportingAPI.BusinessLogic;

public sealed class FileImportReportingService : IFileImportReportingService
{
    private readonly IDbContextResolver<EstateManagementContext> Resolver;
    private const string EstateManagementDatabaseName = "TransactionProcessorReadModel";
    public FileImportReportingService(IDbContextResolver<EstateManagementContext> resolver) => Resolver = resolver;

    public async Task<Result<List<FileImportLog>>> GetFileImportLogList(FileImportLogQueries.GetFileImportLogListQuery request,
                                                                        CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;
        // Build flat projection of the required data then assemble into hierarchical models in-memory
        var flatQuery = from fil in context.FileImportLogs
                        join f in context.Files on fil.FileImportLogId equals f.FileImportLogId
                        join esu in context.EstateSecurityUsers on f.UserId equals esu.SecurityUserId into esuJoin
                        from esu in esuJoin.DefaultIfEmpty()
                        join m in context.Merchants on f.MerchantId equals m.MerchantId into mJoin
                        from m in mJoin.DefaultIfEmpty()
                        join fl in context.FileLines on f.FileId equals fl.FileId
                        where fil.ImportLogDate >= request.StartDate && fil.ImportLogDate <= request.EndDate
                              && (request.MerchantId == null || f.MerchantId == request.MerchantId)
                        select new FileImportFlatData {
                            FileImportLogId = fil.FileImportLogId,
                            ImportLogDateTime = fil.ImportLogDateTime,
                            FileId = f.FileId,
                            FileName = f.FileLocation,
                            FileProfileId = f.FileProfileId,
                            DateTimeUploaded = f.FileReceivedDateTime,
                            UserId = f.UserId,
                            UploadedBy = esu != null ? esu.EmailAddress : null,
                            MerchantId = f.MerchantId,
                            MerchantName = m != null ? m.Name : null,
                            LineNumber = fl.LineNumber,
                            LineContents = fl.FileLineData,
                            LineStatus = fl.Status
                        };

        var flatResult = await ReportingQueryExecutor.ToListAsync(flatQuery, cancellationToken, "Error retrieving file import logs");

        if (flatResult.IsFailed)
            return ResultHelpers.CreateFailure(flatResult);

        var flatItems = flatResult.Data;

        // assemble hierarchical model
        var fileImportLogs = flatItems
            .GroupBy(x => new { x.FileImportLogId, x.ImportLogDateTime })
            .Select(g => new FileImportLog {
                FileImportLogId = g.Key.FileImportLogId,
                ImportLogDateTime = g.Key.ImportLogDateTime,
                FileDetailsList = g.GroupBy(f => new { f.FileId, f.FileName, f.FileProfileId, f.DateTimeUploaded, f.UserId, f.UploadedBy, f.MerchantId, f.MerchantName })
                    .Select(fg => new FileDetails {
                        FileId = fg.Key.FileId,
                        FileName = fg.Key.FileName,
                        FileProfile = fg.Key.FileProfileId != null ? fg.Key.FileProfileId.ToString() : null,
                        DateTimeUploaded = fg.Key.DateTimeUploaded,
                        UserId = fg.Key.UserId,
                        UploadedBy = fg.Key.UploadedBy,
                        MerchantId = fg.Key.MerchantId,
                        MerchantName = fg.Key.MerchantName,
                        FileLines = fg.Select(fl => new FileLine {
                            LineNumber = fl.LineNumber,
                            LineContents = fl.LineContents,
                            LineStatus = fl.LineStatus
                        }).ToList()
                    }).ToList()
            }).ToList();

        return Result.Success(fileImportLogs);
    }

    public async Task<Result<FileImportLog>> GetFileImportLog(FileImportLogQueries.GetFileImportLogQuery request,
                                                                        CancellationToken cancellationToken)
    {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;
        // Build flat projection of the required data then assemble into hierarchical models in-memory
        var flatQuery = from fil in context.FileImportLogs
                        join f in context.Files on fil.FileImportLogId equals f.FileImportLogId
                        join esu in context.EstateSecurityUsers on f.UserId equals esu.SecurityUserId into esuJoin
                        from esu in esuJoin.DefaultIfEmpty()
                        join m in context.Merchants on f.MerchantId equals m.MerchantId into mJoin
                        from m in mJoin.DefaultIfEmpty()
                        join fl in context.FileLines on f.FileId equals fl.FileId
                        //join fp in context.FileProfileConfigurations on f.FileProfileId equals fp.FileProfileId
                        where fil.FileImportLogId == request.FileImportLogId
                              && (request.MerchantId == null || f.MerchantId == request.MerchantId)
                        select new FileImportFlatData
                        {
                            FileImportLogId = fil.FileImportLogId,
                            ImportLogDateTime = fil.ImportLogDateTime,
                            FileId = f.FileId,
                            FileName = f.FileLocation,
                            FileProfileId = f.FileProfileId,
                            DateTimeUploaded = f.FileReceivedDateTime,
                            UserId = f.UserId,
                            UploadedBy = esu != null ? esu.EmailAddress : null,
                            MerchantId = f.MerchantId,
                            MerchantName = m != null ? m.Name : null,
                            LineNumber = fl.LineNumber,
                            LineContents = fl.FileLineData,
                            LineStatus = fl.Status
                        };

        var flatResult = await ReportingQueryExecutor.ToListAsync(flatQuery, cancellationToken, "Error retrieving file import logs");

        if (flatResult.IsFailed)
            return ResultHelpers.CreateFailure(flatResult);

        var flatItems = flatResult.Data;

        if (flatItems.Count == 0)
            return Result.NotFound();
        
        var fileImportLogs = flatItems
            .GroupBy(x => new { x.FileImportLogId, x.ImportLogDateTime })
            .Select(g => new FileImportLog
            {
                FileImportLogId = g.Key.FileImportLogId,
                ImportLogDateTime = g.Key.ImportLogDateTime,
                FileDetailsList = g.GroupBy(f => new { f.FileId, f.FileName, f.FileProfileId, f.DateTimeUploaded, f.UserId, f.UploadedBy, f.MerchantId, f.MerchantName })
                    .Select(fg => new FileDetails
                    {
                        FileId = fg.Key.FileId,
                        FileName = fg.Key.FileName,
                        FileProfile = fg.Key.FileProfileId != null ? fg.Key.FileProfileId.ToString() : null,
                        DateTimeUploaded = fg.Key.DateTimeUploaded,
                        UserId = fg.Key.UserId,
                        UploadedBy = fg.Key.UploadedBy,
                        MerchantId = fg.Key.MerchantId,
                        MerchantName = fg.Key.MerchantName,
                        FileLines = fg.Select(fl => new FileLine
                        {
                            LineNumber = fl.LineNumber,
                            LineContents = fl.LineContents,
                            LineStatus = fl.LineStatus
                        }).ToList()
                    }).ToList()
            }).SingleOrDefault();
        
        return Result.Success(fileImportLogs);
    }

    public async Task<Result<List<FileProfileConfiguration>>> GetFileProfileConfigurationList(FileProfileConfigurationQueries.GetFileProfileConfigurationListQuery request,
                                                                                              CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var query = from c in context.FileProfileConfigurations
            join h in context.FileFormatHandlers on c.FileFormatHandlerId equals h.FileFormatHandlerId
            join r in context.RequestTypes on c.RequestTypeId equals r.RequestTypeId
            join o in context.Operators on c.OperatorId equals o.OperatorId
            select new FileProfileConfiguration
            {
                FileProfileId = c.FileProfileId,
                Name = c.Name,
                ListeningDirectory = c.ListeningDirectory,
                RequestType = r.Name,
                OperatorName = o.Name,
                LineTerminator = c.LineTerminator,
                FileFormatHandler = h.Name
            };

        Result<List<FileProfileConfiguration>> result = await ReportingQueryExecutor.ToListAsync(query, cancellationToken, "Error retrieving file import logs");

        if (result.IsFailed)
            return ResultHelpers.CreateFailure(result);

        return Result.Success(result.Data);
    }
        private sealed class FileImportFlatData {
            public Guid FileImportLogId { get; init; }
            public DateTime ImportLogDateTime { get; init; }
            public Guid FileId { get; init; }
            public string? FileName { get; init; }
            public Guid? FileProfileId { get; init; }
            public DateTime DateTimeUploaded { get; init; }
            public Guid UserId { get; init; }
            public string? UploadedBy { get; init; }
            public Guid MerchantId { get; init; }
            public string? MerchantName { get; init; }
            public int LineNumber { get; init; }
            public string? LineContents { get; init; }
            public string? LineStatus { get; init; }
        }
}