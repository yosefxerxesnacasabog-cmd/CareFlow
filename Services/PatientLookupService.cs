using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CareFlow.Data;
using CareFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Services;

public sealed class PatientLookupService(IDbContextFactory<CareFlowDbContext> contextFactory)
{
    public Task<List<Patient>> GetPatientsAsync() => Task.Run(async () =>
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Patients.AsNoTracking()
            .OrderBy(patient => patient.FullName)
            .ThenBy(patient => patient.Id)
            .ToListAsync();
    });
}
