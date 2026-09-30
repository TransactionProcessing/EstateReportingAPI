using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Common;
using EstateReportingAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Results.Web;
using SimpleResults;

namespace EstateReportingAPI.Handlers;

public static class FileProfileConfigurationHandler
{
    public static async Task<IResult> GetFileProfileConfigurationList(IEstateContext estateContext,
                                                           IMediator mediator,
                                                           CancellationToken cancellationToken)
    {
        FileProfileConfigurationQueries.GetFileProfileConfigurationListQuery query = new(estateContext.EstateId);
        Result<List<FileProfileConfiguration>> result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r => r.Select(m => new DataTransferObjects.FileProfileConfiguration()
        {
            FileFormatHandler = m.FileFormatHandler,
            FileProfileId = m.FileProfileId,
            LineTerminator = m.LineTerminator,
            ListeningDirectory = m.ListeningDirectory,
            Name = m.Name,
            OperatorName = m.OperatorName,
            RequestType = m.RequestType
        }).ToList());
    }
}

public static class FileImportHandler
{
    public static async Task<IResult> GetFileImportLogList(IEstateContext estateContext,
                                                           [FromQuery] Guid? merchantId,
                                                           [FromQuery] DateOnly startDate,
                                                           [FromQuery] DateOnly endDate,
                                                           BusinessLogic.ReportingDatePolicy datePolicy,
                                                           IMediator mediator,
                                                           CancellationToken cancellationToken)
    {
        FileImportLogQueries.GetFileImportLogListQuery query = new(estateContext.EstateId, merchantId, startDate.ToDateTime(TimeOnly.MinValue), endDate.ToDateTime(TimeOnly.MinValue));
        Result<List<FileImportLog>> result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r => r.Select(m => new DataTransferObjects.FileImportLog
        {
            FileImportLogId = m.FileImportLogId,
            ImportLogDateTime = datePolicy.ToDateTimeOffset(m.ImportLogDateTime),
            FileDetailsList = m.FileDetailsList.Select(fd => new DataTransferObjects.FileDetails {
                DateTimeUploaded = datePolicy.ToDateTimeOffset(fd.DateTimeUploaded),
                FileId = fd.FileId,
                FileName = fd.FileName,
                FileProfile = fd.FileProfile,
                MerchantId = fd.MerchantId,
                MerchantName = fd.MerchantName,
                UploadedBy = fd.UploadedBy,
                UserId = fd.UserId,
                FileLines = fd.FileLines.Select(fl => new DataTransferObjects.FileLine {
                    LineContents = fl.LineContents,
                    LineNumber = fl.LineNumber,
                    LineStatus = fl.LineStatus
                }).ToList()
            }).ToList()
        }).ToList());
    }

    public static async Task<IResult> GetFileImportLog(IEstateContext estateContext,
                                                       [FromRoute] Guid fileImportLogId,
                                                           [FromQuery] Guid? merchantId,
                                                            BusinessLogic.ReportingDatePolicy datePolicy,
                                                           IMediator mediator,
                                                           CancellationToken cancellationToken)
    {
        FileImportLogQueries.GetFileImportLogQuery query = new(estateContext.EstateId, merchantId, fileImportLogId);
        Result<FileImportLog> result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r => new DataTransferObjects.FileImportLog
        {
            FileImportLogId = r.FileImportLogId,
            ImportLogDateTime = datePolicy.ToDateTimeOffset(r.ImportLogDateTime),
            FileDetailsList = r.FileDetailsList.Select(fd => new DataTransferObjects.FileDetails
            {
                DateTimeUploaded = datePolicy.ToDateTimeOffset(fd.DateTimeUploaded),
                FileId = fd.FileId,
                FileName = Path.GetFileName(fd.FileName),
                FileProfile = fd.FileProfile,
                MerchantId = fd.MerchantId,
                MerchantName = fd.MerchantName,
                UploadedBy = fd.UploadedBy,
                UserId = fd.UserId,
                FileLines = fd.FileLines.Select(fl => new DataTransferObjects.FileLine
                {
                    LineContents = fl.LineContents,
                    LineNumber = fl.LineNumber,
                    LineStatus = fl.LineStatus
                }).ToList()
            }).ToList()
        });
    }
}
