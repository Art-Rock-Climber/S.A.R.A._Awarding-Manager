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
            AppDbContext.InitializeDatabase(context);

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
                var wsReasons = package.Workbook.Worksheets["Основания"];
                var wsAwards = package.Workbook.Worksheets["Награды"];
                var wsDecrees = package.Workbook.Worksheets["Постановления"];
                var wsAwarded = package.Workbook.Worksheets["Награждаемые"];

                bool isMultiSheetRelational = wsReasons != null && wsAwards != null && wsDecrees != null && wsAwarded != null;

                if (isMultiSheetRelational)
                {
                    ImportMultiSheetRelational(package, context);
                }
                else
                {
                    ImportTurnkeySingleSheet(package, context);
                }
            }

            transaction.Commit();
        }

        private void ImportMultiSheetRelational(ExcelPackage package, AppDbContext context)
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
                    string name = nameVal.ToString()!.Trim();

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
                    string name = nameVal.ToString()!.Trim();

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
                    string number = numberVal.ToString()!.Trim();
                    DateTime date = ParseExcelDate(dateVal);
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
                    string name = nameVal.ToString()!.Trim();

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
                    string lastName = lastNameVal.ToString()!.Trim();
                    string firstName = firstNameVal.ToString()!.Trim();
                    string middleName = middleNameVal?.ToString()?.Trim() ?? string.Empty;
                    string position = positionVal.ToString()!.Trim();
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
                            MiddleName = string.IsNullOrEmpty(middleName) ? null : middleName,
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

        private void ImportTurnkeySingleSheet(ExcelPackage package, AppDbContext context)
        {
            var ws = package.Workbook.Worksheets["Награждения"] ?? package.Workbook.Worksheets.FirstOrDefault();
            if (ws == null) return;

            int rowCount = ws.Dimension?.End.Row ?? 0;
            int colCount = ws.Dimension?.End.Column ?? 0;
            if (rowCount < 2) return;

            // Header auto-detection
            int colAwardedName = -1, colType = -1, colPosition = -1, colCollective = -1;
            int colAward = -1, colReason = -1, colDecreeNum = -1, colDecreeDate = -1;

            for (int col = 1; col <= colCount; col++)
            {
                string header = ws.Cells[1, col].Value?.ToString()?.Trim().ToLower() ?? "";
                if (string.IsNullOrEmpty(header)) continue;

                if (header.Contains("фио") || header.Contains("награждаем") || header.Contains("гражданин") || header.Contains("название"))
                    colAwardedName = col;
                else if (header.Contains("тип"))
                    colType = col;
                else if (header.Contains("должност"))
                    colPosition = col;
                else if (header.Contains("коллектив") || header.Contains("организац") || header.Contains("предприят"))
                    colCollective = col;
                else if (header.Contains("наград"))
                    colAward = col;
                else if (header.Contains("основан") || header.Contains("причин"))
                    colReason = col;
                else if (header.Contains("номер") || header.Contains("постановл") || header.Contains("приказ"))
                    colDecreeNum = col;
                else if (header.Contains("дат"))
                    colDecreeDate = col;
            }

            // Fallback positional mapping if headers not detected
            if (colAwardedName == -1) colAwardedName = 1;
            if (colType == -1) colType = 2;
            if (colPosition == -1) colPosition = 3;
            if (colAward == -1) colAward = 4;
            if (colReason == -1) colReason = 5;
            if (colDecreeNum == -1) colDecreeNum = 6;
            if (colDecreeDate == -1) colDecreeDate = 7;
            if (colCollective == -1) colCollective = 8;

            for (int row = 2; row <= rowCount; row++)
            {
                string awardedName = ws.Cells[row, colAwardedName].Value?.ToString()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(awardedName)) continue;

                string typeStr = colType <= colCount ? ws.Cells[row, colType].Value?.ToString()?.Trim() ?? "" : "";
                string position = colPosition <= colCount ? ws.Cells[row, colPosition].Value?.ToString()?.Trim() ?? "" : "";
                string awardName = colAward <= colCount ? ws.Cells[row, colAward].Value?.ToString()?.Trim() ?? "" : "";
                string reasonName = colReason <= colCount ? ws.Cells[row, colReason].Value?.ToString()?.Trim() ?? "" : "";
                string decreeNum = colDecreeNum <= colCount ? ws.Cells[row, colDecreeNum].Value?.ToString()?.Trim() ?? "" : "";
                object? rawDateVal = colDecreeDate <= colCount ? ws.Cells[row, colDecreeDate].Value : null;
                string collectiveName = colCollective <= colCount ? ws.Cells[row, colCollective].Value?.ToString()?.Trim() ?? "" : "";

                if (string.IsNullOrWhiteSpace(awardName)) continue;

                // 1. AwardReason (on-the-fly)
                if (string.IsNullOrWhiteSpace(reasonName)) reasonName = "По согласованию";
                var reason = context.AwardReasons.FirstOrDefault(r => r.ReasonName == reasonName);
                if (reason == null)
                {
                    reason = new AwardReason { ReasonName = reasonName };
                    context.AwardReasons.Add(reason);
                    context.SaveChanges();
                }

                // 2. Award (on-the-fly)
                var award = context.Awards.FirstOrDefault(a => a.AwardName == awardName);
                if (award == null)
                {
                    award = new Award { AwardName = awardName };
                    context.Awards.Add(award);
                    context.SaveChanges();
                }

                // 3. Decree (on-the-fly)
                if (string.IsNullOrWhiteSpace(decreeNum)) decreeNum = "Б/Н";
                DateTime decreeDate = ParseExcelDate(rawDateVal);

                var decree = context.Decrees.FirstOrDefault(d => d.Number == decreeNum && d.Date.Date == decreeDate.Date);
                if (decree == null)
                {
                    decree = new Decree
                    {
                        Number = decreeNum,
                        Date = decreeDate,
                        AwardReasonId = reason.Id
                    };
                    context.Decrees.Add(decree);
                    context.SaveChanges();
                }

                // 4. Collective (on-the-fly)
                Collective? collectiveEntity = null;
                if (!string.IsNullOrWhiteSpace(collectiveName))
                {
                    collectiveEntity = context.Awarded.OfType<Collective>().FirstOrDefault(c => c.CollectiveName == collectiveName);
                    if (collectiveEntity == null)
                    {
                        collectiveEntity = new Collective { CollectiveName = collectiveName };
                        context.Awarded.Add(collectiveEntity);
                        context.SaveChanges();
                    }
                }

                // 5. Awarded (Citizen / Collective on-the-fly)
                int awardedId;
                bool isCollective = typeStr.Equals("Коллектив", StringComparison.OrdinalIgnoreCase) ||
                                   (string.IsNullOrWhiteSpace(position) && !awardedName.Contains(' '));

                if (isCollective)
                {
                    var coll = context.Awarded.OfType<Collective>().FirstOrDefault(c => c.CollectiveName == awardedName);
                    if (coll == null)
                    {
                        coll = new Collective { CollectiveName = awardedName };
                        context.Awarded.Add(coll);
                        context.SaveChanges();
                    }
                    awardedId = coll.Id;
                }
                else
                {
                    // Parse FIO
                    string lastName = awardedName;
                    string firstName = "-";
                    string? middleName = null;

                    string[] parts = awardedName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 1) lastName = parts[0];
                    if (parts.Length >= 2) firstName = parts[1];
                    if (parts.Length >= 3) middleName = string.Join(' ', parts.Skip(2));

                    if (string.IsNullOrWhiteSpace(position)) position = "-";

                    var citizen = context.Awarded.OfType<Citizen>().FirstOrDefault(c =>
                        c.LastName == lastName && c.FirstName == firstName && c.Position == position);

                    if (citizen == null)
                    {
                        citizen = new Citizen
                        {
                            LastName = lastName,
                            FirstName = firstName,
                            MiddleName = middleName,
                            Position = position,
                            CollectiveId = collectiveEntity?.Id
                        };
                        context.Awarded.Add(citizen);
                        context.SaveChanges();
                    }
                    awardedId = citizen.Id;
                }

                // 6. AwardAssignment (on-the-fly)
                var existingAssignment = context.AwardAssignments.FirstOrDefault(aa =>
                    aa.AwardedId == awardedId && aa.AwardId == award.Id && aa.DecreeId == decree.Id);

                if (existingAssignment == null)
                {
                    var newAssignment = new AwardAssignment
                    {
                        AwardedId = awardedId,
                        AwardId = award.Id,
                        DecreeId = decree.Id
                    };
                    context.AwardAssignments.Add(newAssignment);
                    context.SaveChanges();
                }
            }
        }

        private static DateTime ParseExcelDate(object? rawValue)
        {
            if (rawValue == null) return DateTime.Today;

            // 1. Direct DateTime cell from EPPlus
            if (rawValue is DateTime dt)
            {
                return dt;
            }

            // 2. Numeric Excel OADate (e.g. 43839 or 43839.0)
            if (rawValue is double dbl && dbl > 1000 && dbl < 2958465)
            {
                return DateTime.FromOADate(dbl);
            }
            if (rawValue is int integer && integer > 1000 && integer < 2958465)
            {
                return DateTime.FromOADate(integer);
            }
            if (rawValue is long lng && lng > 1000 && lng < 2958465)
            {
                return DateTime.FromOADate(lng);
            }

            string str = rawValue.ToString()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(str)) return DateTime.Today;

            // 3. String containing OADate numeric value (e.g. "43839")
            if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedDbl))
            {
                if (parsedDbl > 1000 && parsedDbl < 2958465)
                {
                    return DateTime.FromOADate(parsedDbl);
                }
            }

            // 4. Standard Date String formats
            string[] formats = { "dd.MM.yyyy", "dd.MM.yyyy H:mm:ss", "yyyy-MM-dd", "dd/MM/yyyy", "d.M.yyyy" };
            if (DateTime.TryParseExact(str, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime exactDate))
            {
                return exactDate;
            }

            if (DateTime.TryParse(str, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                return parsedDate;
            }

            return DateTime.Today;
        }
    }
}
