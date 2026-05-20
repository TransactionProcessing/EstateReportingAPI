using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Results.Web;
using SimpleResults;

namespace EstateReportingAPI.Handlers;

public static class FileImportHandler
{
    public static async Task<IResult> GetFileImportLogList([FromHeader] Guid estateId,
                                                           [FromQuery] Guid? merchantId,
                                                           [FromQuery] DateTime startDate,
                                                           [FromQuery] DateTime endDate,
                                                           IMediator mediator,
                                                           CancellationToken cancellationToken)
    {
        FileImportLogQueries.GetFileImportLogListQuery query = new(estateId, merchantId, startDate, endDate);
        Result<List<FileImportLog>> result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r => r.Select(m => new DataTransferObjects.FileImportLog
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
}