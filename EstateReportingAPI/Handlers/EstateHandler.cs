using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Results.Web;
using SimpleResults;

namespace EstateReportingAPI.Handlers;

public static class EstateHandler {
    public static async Task<IResult> GetEstate([FromHeader] Guid estateId,
                                                IMediator mediator,
                                                CancellationToken cancellationToken)
    {
        EstateQueries.GetEstateQuery query = new EstateQueries.GetEstateQuery(estateId);
        Result<Estate> result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r => {
            DataTransferObjects.Estate estate = new DataTransferObjects.Estate {
                EstateId = r.EstateId,
                EstateName = r.EstateName,
                Reference = r.Reference,
                Contracts = new List<DataTransferObjects.EstateContract>(),
                Merchants = new List<DataTransferObjects.EstateMerchant>(),
                Operators = new List<DataTransferObjects.EstateOperator>(),
                Users = new List<DataTransferObjects.EstateUser>()
            };

            foreach (var contract in r.Contracts) {
                estate.Contracts.Add(new DataTransferObjects.EstateContract {
                    ContractId = contract.ContractId,
                    Name = contract.Name,
                });
            }

            foreach (var merchant in r.Merchants) {
                estate.Merchants.Add(new DataTransferObjects.EstateMerchant {
                    MerchantId = merchant.MerchantId,
                    Name = merchant.Name,
                    Reference = merchant.Reference
                });
            }

            foreach (Models.EstateOperator estateOperator in r.Operators) {
                estate.Operators.Add(new DataTransferObjects.EstateOperator() {
                    Name = estateOperator.Name,
                    OperatorId = estateOperator.OperatorId
                });
            }

            foreach (Models.EstateUser estateUser in r.Users) {
                estate.Users.Add(new DataTransferObjects.EstateUser() {
                    CreatedDateTime = estateUser.CreatedDateTime,
                    EmailAddress = estateUser.EmailAddress,
                    UserId = estateUser.UserId
                });
            }

            return estate;
        });
    }

    public static async Task<IResult> GetOperators([FromHeader] Guid estateId,
                                                   IMediator mediator,
                                                   CancellationToken cancellationToken)
    {
        EstateQueries.GetEstateOperatorsQuery query = new EstateQueries.GetEstateOperatorsQuery(estateId);
        Result<List<Models.EstateOperator>> result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r => {
            List<EstateOperator> operators = new();

            foreach (Models.EstateOperator estateOperator in r) {
                operators.Add(new() {
                    Name = estateOperator.Name,
                    OperatorId = estateOperator.OperatorId
                });
            }

            return operators;
        });
    }
}