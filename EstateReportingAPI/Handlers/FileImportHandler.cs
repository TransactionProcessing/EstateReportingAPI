using EstateReportingAPI.BusinessLogic.Queries;
using System.ComponentModel;
using EstateReportingAPI.Common;
using EstateReportingAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TransactionProcessing.UnionResults.AspNetCore;
using TransactionProcessing.UnionResults;

namespace EstateReportingAPI.Handlers;

public static class FileProfileConfigurationHandler
{
    public static async Task<Result<List<DataTransferObjects.FileProfileConfiguration>>> GetFileProfileConfigurationList(IEstateContext estateContext,
                                                           IMediator mediator,
                                                           CancellationToken cancellationToken)
    {
        FileProfileConfigurationQueries.GetFileProfileConfigurationListQuery query = new(estateContext.EstateId);
        Result<List<FileProfileConfiguration>> result = await mediator.Send(query, cancellationToken);

        return result.Map(r => r.Select(m => new DataTransferObjects.FileProfileConfiguration()
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
    public static async Task<Result<List<DataTransferObjects.FileImportLog>>> GetFileImportLogList(IEstateContext estateContext,
                                                           [FromQuery, Description("Optional merchant identifier used to filter import logs.")] Guid? merchantId,
                                                           [FromQuery, Description("Inclusive first date in the import log period (YYYY-MM-DD).") ] DateOnly startDate,
                                                           [FromQuery, Description("Inclusive last date in the import log period (YYYY-MM-DD).") ] DateOnly endDate,
                                                           BusinessLogic.ReportingDatePolicy datePolicy,
                                                           IMediator mediator,
                                                           CancellationToken cancellationToken)
    {
        FileImportLogQueries.GetFileImportLogListQuery query = new(estateContext.EstateId, merchantId, startDate, endDate);
        Result<List<FileImportLog>> result = await mediator.Send(query, cancellationToken);

        return result.Map(r => r.Select(m => new DataTransferObjects.FileImportLog
        {
            FileImportLogId = m.FileImportLogId,
            ImportLogDateTime = m.ImportLogDateTime,
            FileDetailsList = m.FileDetailsList.Select(fd => new DataTransferObjects.FileDetails {
                DateTimeUploaded = fd.DateTimeUploaded,
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

    public static async Task<Result<DataTransferObjects.FileImportLog>> GetFileImportLog(IEstateContext estateContext,
                                                       [FromRoute, Description("Unique identifier of the file import log.")] Guid fileImportLogId,
                                                           [FromQuery, Description("Optional merchant identifier used to filter the import log.")] Guid? merchantId,
                                                            BusinessLogic.ReportingDatePolicy datePolicy,
                                                           IMediator mediator,
                                                           CancellationToken cancellationToken)
    {
        FileImportLogQueries.GetFileImportLogQuery query = new(estateContext.EstateId, merchantId, fileImportLogId);
        Result<FileImportLog> result = await mediator.Send(query, cancellationToken);

        return result.Map(r => new DataTransferObjects.FileImportLog
        {
            FileImportLogId = r.FileImportLogId,
            ImportLogDateTime = r.ImportLogDateTime,
            FileDetailsList = r.FileDetailsList.Select(fd => new DataTransferObjects.FileDetails
            {
                DateTimeUploaded = fd.DateTimeUploaded,
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
