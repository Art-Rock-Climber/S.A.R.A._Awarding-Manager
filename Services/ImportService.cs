using OfficeOpenXml;
using sara_coursework.data;
using sara_coursework.models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace sara_coursework.Services
{
    public interface IImportService
    {
        void ImportDatabaseFromExcel(string filePath, bool clearDatabase);
    }

    public class ImportService : IImportService
    {
        public void ImportDatabaseFromExcel(string filePath, bool clearDatabase)
        {
            using var context = new AppDbContext();
            using var transaction = context.Database.BeginTransaction();

            if (clearDatabase)
            {
                context.AwardAssignments.RemoveRange(context.AwardAssignments);
                context.Awarded.RemoveRange(context.Awarded);
                context.Decrees.RemoveRange(context.Decrees);
                context.Awards.RemoveRange(context.Awards);
                context.AwardReasons.RemoveRange(context.AwardReasons);
                context.SaveChanges();
            }

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                // 1. Import AwardReasons
                var reasonIdMap = new Dictionary<int, int>();
                var wsReasons = package.Workbook.Worksheets["Основания"];
                if (wsReasons != null)
                {
                    int rowCount = wsReasons.Dimension?.End.Row ?? 0;
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var idVal = wsReasons.Cells[row, 1].Value;
                        var nameVal = wsReasons.Cells[row, 2].Value;
                        if (idVal == null || nameVal == null) continue;

                        int oldId = Convert.ToInt32(idVal);
                        string name = nameVal.ToString().Trim();

                        var existing = context.AwardReasons.FirstOrDefault(r => r.ReasonName == name);
                        if (existing == null)
                        {
                            var newReason = new AwardReason { ReasonName = name };
                            context.AwardReasons.Add(newReason);
                            context.SaveChanges();
                            reasonIdMap[oldId] = newReason.Id;
                        }
                        else
                        {
                            reasonIdMap[oldId] = existing.Id;
                        }
                    }
                }

                // 2. Import Awards
                var awardIdMap = new Dictionary<int, int>();
                var wsAwards = package.Workbook.Worksheets["Награды"];
                if (wsAwards != null)
                {
                    int rowCount = wsAwards.Dimension?.End.Row ?? 0;
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var idVal = wsAwards.Cells[row, 1].Value;
                        var nameVal = wsAwards.Cells[row, 2].Value;
                        if (idVal == null || nameVal == null) continue;

                        int oldId = Convert.ToInt32(idVal);
                        string name = nameVal.ToString().Trim();

                        var existing = context.Awards.FirstOrDefault(a => a.AwardName == name);
                        if (existing == null)
                        {
                            var newAward = new Award { AwardName = name };
                            context.Awards.Add(newAward);
                            context.SaveChanges();
                            awardIdMap[oldId] = newAward.Id;
                        }
                        else
                        {
                            awardIdMap[oldId] = existing.Id;
                        }
                    }
                }

                // 3. Import Decrees
                var decreeIdMap = new Dictionary<int, int>();
                var wsDecrees = package.Workbook.Worksheets["Постановления"];
                if (wsDecrees != null)
                {
                    int rowCount = wsDecrees.Dimension?.End.Row ?? 0;
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var idVal = wsDecrees.Cells[row, 1].Value;
                        var numberVal = wsDecrees.Cells[row, 2].Value;
                        var dateVal = wsDecrees.Cells[row, 3].Value;
                        var reasonIdVal = wsDecrees.Cells[row, 4].Value;

                        if (idVal == null || numberVal == null || dateVal == null || reasonIdVal == null) continue;

                        int oldId = Convert.ToInt32(idVal);
                        string number = numberVal.ToString().Trim();
                        DateTime date;
                        if (!DateTime.TryParseExact(dateVal.ToString(), "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                        {
                            if (!DateTime.TryParse(dateVal.ToString(), out date))
                            {
                                continue;
                            }
                        }
                        int oldReasonId = Convert.ToInt32(reasonIdVal);

                        if (!reasonIdMap.TryGetValue(oldReasonId, out int newReasonId))
                        {
                            continue;
                        }

                        var existing = context.Decrees.FirstOrDefault(d => d.Number == number && d.Date.Date == date.Date);
                        if (existing == null)
                        {
                            var newDecree = new Decree
                            {
                                Number = number,
                                Date = date,
                                AwardReasonId = newReasonId
                            };
                            context.Decrees.Add(newDecree);
                            context.SaveChanges();
                            decreeIdMap[oldId] = newDecree.Id;
                        }
                        else
                        {
                            decreeIdMap[oldId] = existing.Id;
                        }
                    }
                }

                // 4. Import Awarded
                var awardedIdMap = new Dictionary<int, int>();
                var wsAwarded = package.Workbook.Worksheets["Награждаемые"];
                if (wsAwarded != null)
                {
                    int rowCount = wsAwarded.Dimension?.End.Row ?? 0;

                    // Pass 1: Collectives
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var typeVal = wsAwarded.Cells[row, 2].Value;
                        if (typeVal == null || typeVal.ToString() != "Коллектив") continue;

                        var idVal = wsAwarded.Cells[row, 1].Value;
                        var nameVal = wsAwarded.Cells[row, 3].Value;
                        if (idVal == null || nameVal == null) continue;

                        int oldId = Convert.ToInt32(idVal);
                        string name = nameVal.ToString().Trim();

                        var existing = context.Awarded.OfType<Collective>().FirstOrDefault(c => c.CollectiveName == name);
                        if (existing == null)
                        {
                            var newCollective = new Collective { CollectiveName = name };
                            context.Awarded.Add(newCollective);
                            context.SaveChanges();
                            awardedIdMap[oldId] = newCollective.Id;
                        }
                        else
                        {
                            awardedIdMap[oldId] = existing.Id;
                        }
                    }

                    // Pass 2: Citizens
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var typeVal = wsAwarded.Cells[row, 2].Value;
                        if (typeVal == null || typeVal.ToString() != "Гражданин") continue;

                        var idVal = wsAwarded.Cells[row, 1].Value;
                        var lastNameVal = wsAwarded.Cells[row, 4].Value;
                        var firstNameVal = wsAwarded.Cells[row, 5].Value;
                        var middleNameVal = wsAwarded.Cells[row, 6].Value;
                        var positionVal = wsAwarded.Cells[row, 7].Value;
                        var collectiveIdVal = wsAwarded.Cells[row, 8].Value;

                        if (idVal == null || lastNameVal == null || firstNameVal == null || positionVal == null) continue;

                        int oldId = Convert.ToInt32(idVal);
                        string lastName = lastNameVal.ToString().Trim();
                        string firstName = firstNameVal.ToString().Trim();
                        string middleName = middleNameVal?.ToString()?.Trim();
                        string position = positionVal.ToString().Trim();
                        int? oldCollectiveId = collectiveIdVal != null ? (int?)Convert.ToInt32(collectiveIdVal) : null;

                        int? newCollectiveId = null;
                        if (oldCollectiveId.HasValue && awardedIdMap.TryGetValue(oldCollectiveId.Value, out int mappedCollId))
                        {
                            newCollectiveId = mappedCollId;
                        }

                        var existing = context.Awarded.OfType<Citizen>().FirstOrDefault(c =>
                            c.LastName == lastName && c.FirstName == firstName && c.Position == position);

                        if (existing == null)
                        {
                            var newCitizen = new Citizen
                            {
                                LastName = lastName,
                                FirstName = firstName,
                                MiddleName = middleName,
                                Position = position,
                                CollectiveId = newCollectiveId
                            };
                            context.Awarded.Add(newCitizen);
                            context.SaveChanges();
                            awardedIdMap[oldId] = newCitizen.Id;
                        }
                        else
                        {
                            awardedIdMap[oldId] = existing.Id;
                        }
                    }
                }

                // 5. Import AwardAssignments
                var wsAssignments = package.Workbook.Worksheets["Награждения"];
                if (wsAssignments != null)
                {
                    int rowCount = wsAssignments.Dimension?.End.Row ?? 0;
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var awardedIdVal = wsAssignments.Cells[row, 2].Value;
                        var awardIdVal = wsAssignments.Cells[row, 4].Value;
                        var decreeIdVal = wsAssignments.Cells[row, 6].Value;

                        if (awardedIdVal == null || awardIdVal == null || decreeIdVal == null) continue;

                        int oldAwardedId = Convert.ToInt32(awardedIdVal);
                        int oldAwardId = Convert.ToInt32(awardIdVal);
                        int oldDecreeId = Convert.ToInt32(decreeIdVal);

                        if (!awardedIdMap.TryGetValue(oldAwardedId, out int newAwardedId) ||
                            !awardIdMap.TryGetValue(oldAwardId, out int newAwardId) ||
                            !decreeIdMap.TryGetValue(oldDecreeId, out int newDecreeId))
                        {
                            continue;
                        }

                        var existing = context.AwardAssignments.FirstOrDefault(aa =>
                            aa.AwardedId == newAwardedId && aa.AwardId == newAwardId && aa.DecreeId == newDecreeId);

                        if (existing == null)
                        {
                            var newAssignment = new AwardAssignment
                            {
                                AwardedId = newAwardedId,
                                AwardId = newAwardId,
                                DecreeId = newDecreeId
                            };
                            context.AwardAssignments.Add(newAssignment);
                            context.SaveChanges();
                        }
                    }
                }
            }

            transaction.Commit();
        }
    }
}
