﻿using Com.Danliris.Service.Finishing.Printing.Lib.BusinessLogic.Implementations.DailyOperation;
using Com.Danliris.Service.Finishing.Printing.Lib.BusinessLogic.Interfaces.DailyOperation;
using Com.Danliris.Service.Finishing.Printing.Lib.Models.Daily_Operation;
using Com.Danliris.Service.Production.Lib;
using Com.Danliris.Service.Production.Lib.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Com.Moonlay.NetCore.Lib;
using Com.Danliris.Service.Finishing.Printing.Lib.Models.Master.Machine;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore.Internal;

namespace Com.Danliris.Service.Finishing.Printing.Lib.BusinessLogic.Facades.DailyOperation
{
    public class DailyOperationFacade : IDailyOperationFacade
    {
        private readonly ProductionDbContext DbContext;
        private readonly DbSet<DailyOperationModel> DbSet;
        //private readonly DbSet<MachineModel> DbSetMachine;
        private readonly DailyOperationLogic DailyOperationLogic;
        public DailyOperationFacade(IServiceProvider serviceProvider, ProductionDbContext dbContext)
        {
            this.DbContext = dbContext;
            this.DbSet = DbContext.Set<DailyOperationModel>();
            //this.DbSetMachine = 
            this.DailyOperationLogic = serviceProvider.GetService<DailyOperationLogic>();
        }

        public async Task<int> CreateAsync(DailyOperationModel model)
        {
            do
            {
                model.Code = CodeGenerator.Generate();
            }
            while (DbSet.Any(d => d.Code.Equals(model.Code)));

            this.DailyOperationLogic.CreateModel(model);
            return await DbContext.SaveChangesAsync();
        }

        public async Task<int> DeleteAsync(int id)
        {
            await DailyOperationLogic.DeleteModel(id);
            return await DbContext.SaveChangesAsync();
        }

        public ReadResponse<DailyOperationModel> Read(int page, int size, string order, List<string> select, string keyword, string filter)
        {
            IQueryable<DailyOperationModel> query = DbSet;

            List<string> searchAttributes = new List<string>()
            {
                "Code"
            };
            query = QueryHelper<DailyOperationModel>.Search(query, searchAttributes, keyword);

            if (filter.Contains("process"))
            {
                filter = "{}";
                //List<ExpeditionPosition> positions = new List<ExpeditionPosition> { ExpeditionPosition.SEND_TO_PURCHASING_DIVISION, ExpeditionPosition.SEND_TO_ACCOUNTING_DIVISION, ExpeditionPosition.SEND_TO_CASHIER_DIVISION };
                //Query = Query.Where(p => positions.Contains(p.Position));
            }

            Dictionary<string, object> filterDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(filter);
            query = QueryHelper<DailyOperationModel>.Filter(query, filterDictionary);

            List<string> selectedFields = new List<string>()
                {
                    "Id","Type","GoodOutput","Step","BadOutput","Code","Machine","Kanban","Input","Shift","DateInput","DateOutput","LastModifiedUtc"
                };


            //query = query.Join(DbContext.Machine,
            //    daily => daily.MachineId,
            //    machine => machine.Id,
            //    (daily, machine) => new DailyOperationModel
            //    {

            //    })
            //    .Join(DbContext.Kanbans,
            //    daily => daily.KanbanId,
            //    kanban => kanban.Id,
            //    (daily, kanban) => new DailyOperationModel
            //    {

            //        Kanban = kanban
            //    });
            query = from daily in query
                    join machine in DbContext.Machine on daily.MachineId equals machine.Id
                    join kanban in DbContext.Kanbans on daily.KanbanId equals kanban.Id
                    select new DailyOperationModel
                    {
                        Id=daily.Id,
                        Code=daily.Code,
                        Type = daily.Type,
                        StepProcess = daily.StepProcess,
                        Shift = daily.Shift,
                        Kanban = kanban,
                        Machine = machine,
                        DateInput = daily.DateInput,
                        Input = daily.Input,
                        DateOutput = daily.DateOutput,
                        GoodOutput = daily.GoodOutput,
                        BadOutput = daily.BadOutput,
                        LastModifiedUtc = daily.LastModifiedUtc
                    };


            Dictionary<string, string> orderDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(order);
            query = QueryHelper<DailyOperationModel>.Order(query, orderDictionary);

            Pageable<DailyOperationModel> pageable = new Pageable<DailyOperationModel>(query, page - 1, size);
            List<DailyOperationModel> data = pageable.Data.ToList();
            int totalData = pageable.TotalCount;

            return new ReadResponse<DailyOperationModel>(data, totalData, orderDictionary, selectedFields);
        }

        public async Task<DailyOperationModel> ReadByIdAsync(int id)
        {
            return await DailyOperationLogic.ReadModelById(id);
        }

        public async Task<int> UpdateAsync(int id, DailyOperationModel model)
        {
            this.DailyOperationLogic.UpdateModelAsync(id, model);
            return await DbContext.SaveChangesAsync();
        }
    }
}
