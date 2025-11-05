using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace eegProject.Services
{
    internal sealed class ExportRequest
    {
        public ExportRequest(int userId, string experimentType, IReadOnlyCollection<string> timeLabels, int? sessionId = null)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            UserId = userId;
            ExperimentType = string.IsNullOrWhiteSpace(experimentType) ? null : experimentType.Trim();
            TimeLabels = timeLabels;
            SessionId = sessionId;
        }

        public int UserId { get; }

        public string ExperimentType { get; }

        public IReadOnlyCollection<string> TimeLabels { get; }

        public int? SessionId { get; }

        public bool HasExplicitTimeLabels => TimeLabels != null && TimeLabels.Count > 0;
    }

    internal sealed class ExportService
    {
        private static readonly string[] HeaderTitles =
        {
            "Delta",
            "Theta",
            "LowAlpha",
            "HighAlpha",
            "LowBeta",
            "HighBeta",
            "LowGamma",
            "HighGamma",
            "BlinkStrength",
            "SignalQuality",
            "KayitZamani"
        };

        private static readonly char[] InvalidSheetChars = { '\\', '/', '*', '[', ']', ':', '?' };

        private static Stylesheet CreateStylesheet()
        {
            var stylesheet = new Stylesheet();

            // Fonts
            var fonts = new Fonts();
            fonts.Append(new Font()); // Index 0 - Normal
            fonts.Append(new Font(new Bold())); // Index 1 - Bold
            fonts.Count = (uint)fonts.ChildElements.Count;

            // Fills
            var fills = new Fills();
            fills.Append(new Fill(new PatternFill { PatternType = PatternValues.None })); // Index 0
            fills.Append(new Fill(new PatternFill { PatternType = PatternValues.Gray125 })); // Index 1
            fills.Append(new Fill(new PatternFill(new ForegroundColor { Rgb = "FF4472C4" }) { PatternType = PatternValues.Solid })); // Index 2 - Blue header
            fills.Append(new Fill(new PatternFill(new ForegroundColor { Rgb = "FFE7E6E6" }) { PatternType = PatternValues.Solid })); // Index 3 - Light gray
            fills.Count = (uint)fills.ChildElements.Count;

            // Borders
            var borders = new Borders();
            borders.Append(new Border()); // Index 0 - No border
            borders.Append(new Border( // Index 1 - All borders
                new LeftBorder(new Color { Auto = true }) { Style = BorderStyleValues.Thin },
                new RightBorder(new Color { Auto = true }) { Style = BorderStyleValues.Thin },
                new TopBorder(new Color { Auto = true }) { Style = BorderStyleValues.Thin },
                new BottomBorder(new Color { Auto = true }) { Style = BorderStyleValues.Thin }
            ));
            borders.Count = (uint)borders.ChildElements.Count;

            // Cell formats
            var cellFormats = new CellFormats();
            cellFormats.Append(new CellFormat()); // Index 0 - Default
            cellFormats.Append(new CellFormat { FontId = 1, FillId = 0, BorderId = 0, ApplyFont = true }); // Index 1 - Bold
            cellFormats.Append(new CellFormat { FontId = 1, FillId = 2, BorderId = 1, ApplyFont = true, ApplyFill = true, ApplyBorder = true }); // Index 2 - Header (blue + bold)
            cellFormats.Append(new CellFormat { FontId = 0, FillId = 3, BorderId = 1, ApplyFill = true, ApplyBorder = true }); // Index 3 - Metadata (light gray)
            cellFormats.Append(new CellFormat { FontId = 0, FillId = 0, BorderId = 1, ApplyBorder = true, NumberFormatId = 2 }); // Index 4 - Number with borders
            cellFormats.Append(new CellFormat { FontId = 0, FillId = 0, BorderId = 1, ApplyBorder = true }); // Index 5 - Text with borders
            cellFormats.Count = (uint)cellFormats.ChildElements.Count;

            stylesheet.Fonts = fonts;
            stylesheet.Fills = fills;
            stylesheet.Borders = borders;
            stylesheet.CellFormats = cellFormats;

            return stylesheet;
        }

        public async Task ExportToExcelAsync(ExportRequest request, string filePath, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Dosya yolu zorunludur", nameof(filePath));
            }

            using (var context = DbContextFactory.Create())
            {
                var user = await context.Kullanici
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.KullaniciID == request.UserId, cancellationToken)
                    .ConfigureAwait(false);

                if (user == null)
                {
                    throw new InvalidOperationException("Secilen kullanici bulunamadi.");
                }

                var query = from o in context.Oturum.AsNoTracking()
                            where o.KullaniciID == request.UserId
                            join e in context.EEGVerisi.AsNoTracking() on o.OturumID equals e.OturumID
                            select new ExportRow
                            {
                                SessionId = o.OturumID,
                                TimeLabel = o.ZamanEtiketi,
                                ExperimentType = o.DeneyTuru,
                                Delta = e.Delta,
                                Theta = e.Theta,
                                LowAlpha = e.LowAlpha,
                                HighAlpha = e.HighAlpha,
                                LowBeta = e.LowBeta,
                                HighBeta = e.HighBeta,
                                LowGamma = e.LowGamma,
                                HighGamma = e.HighGamma,
                                BlinkStrength = e.BlinkStrength,
                                TimestampUtc = e.KayitZamani
                            };

                if (!string.IsNullOrWhiteSpace(request.ExperimentType))
                {
                    query = query.Where(row => row.ExperimentType == request.ExperimentType);
                }

                if (request.SessionId.HasValue)
                {
                    var sessionId = request.SessionId.Value;
                    query = query.Where(row => row.SessionId == sessionId);
                }

                if (request.HasExplicitTimeLabels)
                {
                    var explicitLabels = request.TimeLabels;
                    var includeNull = explicitLabels.Contains(null);
                    var nonNull = explicitLabels.Where(label => label != null).ToList();

                    query = query.Where(row =>
                        (includeNull && row.TimeLabel == null) ||
                        nonNull.Contains(row.TimeLabel));
                }

                var rows = await query
                    .OrderBy(row => row.TimeLabel)
                    .ThenBy(row => row.TimestampUtc)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                var grouped = rows
                    .GroupBy(row => NormalizeKey(row.TimeLabel))
                    .ToDictionary(g => g.Key, g => g.ToList());

                IReadOnlyCollection<string> targetKeys;
                if (request.HasExplicitTimeLabels)
                {
                    targetKeys = request.TimeLabels
                        .Select(NormalizeKey)
                        .Distinct()
                        .ToList();
                }
                else
                {
                    targetKeys = grouped.Keys.ToList();
                }

                if (!targetKeys.Any())
                {
                    throw new InvalidOperationException("Secilen filtreler icin EEG verisi bulunamadi.");
                }

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                using (var document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook))
                {
                    var workbookPart = document.AddWorkbookPart();
                    workbookPart.Workbook = new Workbook();
                    
                    // Add stylesheet for formatting
                    var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
                    stylesPart.Stylesheet = CreateStylesheet();
                    stylesPart.Stylesheet.Save();
                    
                    var sheets = workbookPart.Workbook.AppendChild(new Sheets());

                    var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    uint sheetId = 1;
                    var experimentDisplay = string.IsNullOrWhiteSpace(request.ExperimentType) ? "Tumu" : request.ExperimentType;
                    var generatedAt = DateTime.Now;

                    foreach (var key in targetKeys)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var displayLabel = BuildDisplayLabel(key);
                        var sheetName = BuildSheetName(displayLabel, usedNames);
                        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                        var sheetData = new SheetData();
                        
                        // Add column widths
                        var columns = new Columns();
                        columns.Append(new Column { Min = 1, Max = 1, Width = 12, CustomWidth = true }); // Delta
                        columns.Append(new Column { Min = 2, Max = 2, Width = 12, CustomWidth = true }); // Theta
                        columns.Append(new Column { Min = 3, Max = 3, Width = 12, CustomWidth = true }); // LowAlpha
                        columns.Append(new Column { Min = 4, Max = 4, Width = 12, CustomWidth = true }); // HighAlpha
                        columns.Append(new Column { Min = 5, Max = 5, Width = 12, CustomWidth = true }); // LowBeta
                        columns.Append(new Column { Min = 6, Max = 6, Width = 12, CustomWidth = true }); // HighBeta
                        columns.Append(new Column { Min = 7, Max = 7, Width = 12, CustomWidth = true }); // LowGamma
                        columns.Append(new Column { Min = 8, Max = 8, Width = 12, CustomWidth = true }); // HighGamma
                        columns.Append(new Column { Min = 9, Max = 9, Width = 14, CustomWidth = true }); // BlinkStrength
                        columns.Append(new Column { Min = 10, Max = 10, Width = 14, CustomWidth = true }); // SignalQuality
                        columns.Append(new Column { Min = 11, Max = 11, Width = 20, CustomWidth = true }); // KayitZamani
                        
                        worksheetPart.Worksheet = new Worksheet(columns, sheetData);

                        var sheet = new Sheet
                        {
                            Id = workbookPart.GetIdOfPart(worksheetPart),
                            SheetId = sheetId++,
                            Name = sheetName
                        };
                        sheets.Append(sheet);

                        // Metadata row with gray background
                        var metaRow = CreateRow(
                            CreateTextCell("Kullanici", 3),
                            CreateTextCell(user.AdSoyad ?? user.Email ?? user.KullaniciID.ToString(CultureInfo.InvariantCulture), 3),
                            CreateTextCell("Deney Turu", 3),
                            CreateTextCell(experimentDisplay, 3),
                            CreateTextCell("Olusturulma", 3),
                            CreateTextCell(generatedAt.ToString("g", CultureInfo.CurrentCulture), 3)
                        );
                        sheetData.Append(metaRow);

                        // Label row with gray background
                        var labelRow = CreateRow(
                            CreateTextCell("Zaman Etiketi", 3),
                            CreateTextCell(displayLabel, 3)
                        );
                        sheetData.Append(labelRow);

                        sheetData.Append(new Row());

                        // Header row with blue background and bold text
                        var headerRow = CreateRow(HeaderTitles.Select(title => CreateTextCell(title, 2)).ToArray());
                        sheetData.Append(headerRow);

                        if (grouped.TryGetValue(key, out var dataRows) && dataRows.Count > 0)
                        {
                            foreach (var data in dataRows)
                            {
                                var row = CreateRow(
                                    CreateNumberCell(data.Delta, 4),
                                    CreateNumberCell(data.Theta, 4),
                                    CreateNumberCell(data.LowAlpha, 4),
                                    CreateNumberCell(data.HighAlpha, 4),
                                    CreateNumberCell(data.LowBeta, 4),
                                    CreateNumberCell(data.HighBeta, 4),
                                    CreateNumberCell(data.LowGamma, 4),
                                    CreateNumberCell(data.HighGamma, 4),
                                    CreateIntegerCell(data.BlinkStrength, 4),
                                    CreateIntegerCell(null, 4),
                                    CreateTextCell(data.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), 5)
                                );
                                sheetData.Append(row);
                            }
                        }

                        worksheetPart.Worksheet.Save();
                    }

                    workbookPart.Workbook.Save();
                }
            }
        }

        public async Task ExportMultipleUsersToExcelAsync(IEnumerable<int> userIds, string experimentType, IReadOnlyCollection<string> timeLabels, string filePath, CancellationToken cancellationToken = default)
        {
            if (userIds == null || !userIds.Any())
            {
                throw new ArgumentException("En az bir kullanici ID'si gereklidir", nameof(userIds));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Dosya yolu zorunludur", nameof(filePath));
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using (var context = DbContextFactory.Create())
            {
                using (var document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook))
                {
                    var workbookPart = document.AddWorkbookPart();
                    workbookPart.Workbook = new Workbook();
                    
                    // Add stylesheet for formatting
                    var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
                    stylesPart.Stylesheet = CreateStylesheet();
                    stylesPart.Stylesheet.Save();
                    
                    var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                    var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    uint sheetId = 1;
                    var generatedAt = DateTime.Now;
                    var experimentDisplay = string.IsNullOrWhiteSpace(experimentType) ? "Tumu" : experimentType;

                    foreach (var userId in userIds)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var user = await context.Kullanici
                            .AsNoTracking()
                            .FirstOrDefaultAsync(u => u.KullaniciID == userId, cancellationToken)
                            .ConfigureAwait(false);

                        if (user == null)
                        {
                            continue;
                        }

                        var query = from o in context.Oturum.AsNoTracking()
                                    where o.KullaniciID == userId
                                    join e in context.EEGVerisi.AsNoTracking() on o.OturumID equals e.OturumID
                                    select new ExportRow
                                    {
                                        SessionId = o.OturumID,
                                        TimeLabel = o.ZamanEtiketi,
                                        ExperimentType = o.DeneyTuru,
                                        Delta = e.Delta,
                                        Theta = e.Theta,
                                        LowAlpha = e.LowAlpha,
                                        HighAlpha = e.HighAlpha,
                                        LowBeta = e.LowBeta,
                                        HighBeta = e.HighBeta,
                                        LowGamma = e.LowGamma,
                                        HighGamma = e.HighGamma,
                                        BlinkStrength = e.BlinkStrength,
                                        TimestampUtc = e.KayitZamani
                                    };

                        if (!string.IsNullOrWhiteSpace(experimentType))
                        {
                            query = query.Where(row => row.ExperimentType == experimentType);
                        }

                        if (timeLabels != null && timeLabels.Count > 0)
                        {
                            var includeNull = timeLabels.Contains(null);
                            var nonNull = timeLabels.Where(label => label != null).ToList();

                            query = query.Where(row =>
                                (includeNull && row.TimeLabel == null) ||
                                nonNull.Contains(row.TimeLabel));
                        }

                        var rows = await query
                            .OrderBy(row => row.TimeLabel)
                            .ThenBy(row => row.TimestampUtc)
                            .ToListAsync(cancellationToken)
                            .ConfigureAwait(false);

                        var grouped = rows
                            .GroupBy(row => NormalizeKey(row.TimeLabel))
                            .ToDictionary(g => g.Key, g => g.ToList());

                        IReadOnlyCollection<string> targetKeys;
                        if (timeLabels != null && timeLabels.Count > 0)
                        {
                            targetKeys = timeLabels
                                .Select(NormalizeKey)
                                .Distinct()
                                .ToList();
                        }
                        else
                        {
                            targetKeys = grouped.Keys.ToList();
                        }

                        var userName = user.AdSoyad ?? user.Email ?? user.KullaniciID.ToString(CultureInfo.InvariantCulture);

                        foreach (var key in targetKeys)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var displayLabel = BuildDisplayLabel(key);
                            var sheetBaseName = $"{userName}_{displayLabel}";
                            var sheetName = BuildSheetName(sheetBaseName, usedNames);
                            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                            var sheetData = new SheetData();
                            
                            // Add column widths
                            var columns = new Columns();
                            columns.Append(new Column { Min = 1, Max = 1, Width = 12, CustomWidth = true });
                            columns.Append(new Column { Min = 2, Max = 2, Width = 12, CustomWidth = true });
                            columns.Append(new Column { Min = 3, Max = 3, Width = 12, CustomWidth = true });
                            columns.Append(new Column { Min = 4, Max = 4, Width = 12, CustomWidth = true });
                            columns.Append(new Column { Min = 5, Max = 5, Width = 12, CustomWidth = true });
                            columns.Append(new Column { Min = 6, Max = 6, Width = 12, CustomWidth = true });
                            columns.Append(new Column { Min = 7, Max = 7, Width = 12, CustomWidth = true });
                            columns.Append(new Column { Min = 8, Max = 8, Width = 12, CustomWidth = true });
                            columns.Append(new Column { Min = 9, Max = 9, Width = 14, CustomWidth = true });
                            columns.Append(new Column { Min = 10, Max = 10, Width = 14, CustomWidth = true });
                            columns.Append(new Column { Min = 11, Max = 11, Width = 20, CustomWidth = true });
                            
                            worksheetPart.Worksheet = new Worksheet(columns, sheetData);

                            var sheet = new Sheet
                            {
                                Id = workbookPart.GetIdOfPart(worksheetPart),
                                SheetId = sheetId++,
                                Name = sheetName
                            };
                            sheets.Append(sheet);

                            // Metadata row with gray background
                            var metaRow = CreateRow(
                                CreateTextCell("Kullanici", 3),
                                CreateTextCell(userName, 3),
                                CreateTextCell("Deney Turu", 3),
                                CreateTextCell(experimentDisplay, 3),
                                CreateTextCell("Olusturulma", 3),
                                CreateTextCell(generatedAt.ToString("g", CultureInfo.CurrentCulture), 3)
                            );
                            sheetData.Append(metaRow);

                            // Label row with gray background
                            var labelRow = CreateRow(
                                CreateTextCell("Zaman Etiketi", 3),
                                CreateTextCell(displayLabel, 3)
                            );
                            sheetData.Append(labelRow);

                            sheetData.Append(new Row());

                            // Header row with blue background and bold text
                            var headerRow = CreateRow(HeaderTitles.Select(title => CreateTextCell(title, 2)).ToArray());
                            sheetData.Append(headerRow);

                            if (grouped.TryGetValue(key, out var dataRows) && dataRows.Count > 0)
                            {
                                foreach (var data in dataRows)
                                {
                                    var row = CreateRow(
                                        CreateNumberCell(data.Delta, 4),
                                        CreateNumberCell(data.Theta, 4),
                                        CreateNumberCell(data.LowAlpha, 4),
                                        CreateNumberCell(data.HighAlpha, 4),
                                        CreateNumberCell(data.LowBeta, 4),
                                        CreateNumberCell(data.HighBeta, 4),
                                        CreateNumberCell(data.LowGamma, 4),
                                        CreateNumberCell(data.HighGamma, 4),
                                        CreateIntegerCell(data.BlinkStrength, 4),
                                        CreateIntegerCell(null, 4),
                                        CreateTextCell(data.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), 5)
                                    );
                                    sheetData.Append(row);
                                }
                            }

                            worksheetPart.Worksheet.Save();
                        }
                    }

                    workbookPart.Workbook.Save();
                }
            }
        }

        public async Task ExportToJsonAsync(ExportRequest request, string filePath, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Dosya yolu zorunludur", nameof(filePath));
            }

            using (var context = DbContextFactory.Create())
            {
                var user = await context.Kullanici
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.KullaniciID == request.UserId, cancellationToken)
                    .ConfigureAwait(false);

                if (user == null)
                {
                    throw new InvalidOperationException("Secilen kullanici bulunamadi.");
                }

                var query = from o in context.Oturum.AsNoTracking()
                            where o.KullaniciID == request.UserId
                            join e in context.EEGVerisi.AsNoTracking() on o.OturumID equals e.OturumID
                            select new
                            {
                                SessionId = o.OturumID,
                                SessionStart = o.KayitBaslangic,
                                SessionEnd = o.KayitBitis,
                                TimeLabel = o.ZamanEtiketi,
                                ExperimentType = o.DeneyTuru,
                                Notes = o.Notlar,
                                EegData = new
                                {
                                    Delta = e.Delta,
                                    Theta = e.Theta,
                                    LowAlpha = e.LowAlpha,
                                    HighAlpha = e.HighAlpha,
                                    LowBeta = e.LowBeta,
                                    HighBeta = e.HighBeta,
                                    LowGamma = e.LowGamma,
                                    HighGamma = e.HighGamma,
                                    BlinkStrength = e.BlinkStrength,
                                    Timestamp = e.KayitZamani
                                }
                            };

                if (!string.IsNullOrWhiteSpace(request.ExperimentType))
                {
                    query = query.Where(row => row.ExperimentType == request.ExperimentType);
                }

                if (request.SessionId.HasValue)
                {
                    var sessionId = request.SessionId.Value;
                    query = query.Where(row => row.SessionId == sessionId);
                }

                if (request.HasExplicitTimeLabels)
                {
                    var explicitLabels = request.TimeLabels;
                    var includeNull = explicitLabels.Contains(null);
                    var nonNull = explicitLabels.Where(label => label != null).ToList();

                    query = query.Where(row =>
                        (includeNull && row.TimeLabel == null) ||
                        nonNull.Contains(row.TimeLabel));
                }

                var rows = await query
                    .OrderBy(row => row.SessionId)
                    .ThenBy(row => row.EegData.Timestamp)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (!rows.Any())
                {
                    throw new InvalidOperationException("Secilen filtreler icin EEG verisi bulunamadi.");
                }

                var grouped = rows.GroupBy(r => new { r.SessionId, r.TimeLabel, r.ExperimentType, r.SessionStart, r.SessionEnd, r.Notes });

                var exportData = new
                {
                    ExportDate = DateTime.Now,
                    User = new
                    {
                        Id = user.KullaniciID,
                        Name = user.AdSoyad,
                        Email = user.Email,
                        Role = user.Rol
                    },
                    Sessions = grouped.Select(g => new
                    {
                        SessionId = g.Key.SessionId,
                        ExperimentType = g.Key.ExperimentType,
                        TimeLabel = g.Key.TimeLabel,
                        Start = g.Key.SessionStart,
                        End = g.Key.SessionEnd,
                        Notes = g.Key.Notes,
                        EegDataCount = g.Count(),
                        EegData = g.Select(r => r.EegData).ToList()
                    }).ToList()
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(exportData, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
            }
        }

        public async Task ExportMultipleUsersToJsonAsync(IEnumerable<int> userIds, string experimentType, IReadOnlyCollection<string> timeLabels, string filePath, CancellationToken cancellationToken = default)
        {
            if (userIds == null || !userIds.Any())
            {
                throw new ArgumentException("En az bir kullanici ID'si gereklidir", nameof(userIds));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Dosya yolu zorunludur", nameof(filePath));
            }

            using (var context = DbContextFactory.Create())
            {
                var users = await context.Kullanici
                    .AsNoTracking()
                    .Where(u => userIds.Contains(u.KullaniciID))
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (!users.Any())
                {
                    throw new InvalidOperationException("Secilen kullanicilar bulunamadi.");
                }

                var allData = new List<object>();

                foreach (var user in users)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var query = from o in context.Oturum.AsNoTracking()
                                where o.KullaniciID == user.KullaniciID
                                join e in context.EEGVerisi.AsNoTracking() on o.OturumID equals e.OturumID
                                select new
                                {
                                    SessionId = o.OturumID,
                                    SessionStart = o.KayitBaslangic,
                                    SessionEnd = o.KayitBitis,
                                    TimeLabel = o.ZamanEtiketi,
                                    ExperimentType = o.DeneyTuru,
                                    Notes = o.Notlar,
                                    EegData = new
                                    {
                                        Delta = e.Delta,
                                        Theta = e.Theta,
                                        LowAlpha = e.LowAlpha,
                                        HighAlpha = e.HighAlpha,
                                        LowBeta = e.LowBeta,
                                        HighBeta = e.HighBeta,
                                        LowGamma = e.LowGamma,
                                        HighGamma = e.HighGamma,
                                        BlinkStrength = e.BlinkStrength,
                                        Timestamp = e.KayitZamani
                                    }
                                };

                    if (!string.IsNullOrWhiteSpace(experimentType))
                    {
                        query = query.Where(row => row.ExperimentType == experimentType);
                    }

                    if (timeLabels != null && timeLabels.Count > 0)
                    {
                        var includeNull = timeLabels.Contains(null);
                        var nonNull = timeLabels.Where(label => label != null).ToList();

                        query = query.Where(row =>
                            (includeNull && row.TimeLabel == null) ||
                            nonNull.Contains(row.TimeLabel));
                    }

                    var rows = await query
                        .OrderBy(row => row.SessionId)
                        .ThenBy(row => row.EegData.Timestamp)
                        .ToListAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (rows.Any())
                    {
                        var grouped = rows.GroupBy(r => new { r.SessionId, r.TimeLabel, r.ExperimentType, r.SessionStart, r.SessionEnd, r.Notes });

                        allData.Add(new
                        {
                            User = new
                            {
                                Id = user.KullaniciID,
                                Name = user.AdSoyad,
                                Email = user.Email,
                                Role = user.Rol
                            },
                            Sessions = grouped.Select(g => new
                            {
                                SessionId = g.Key.SessionId,
                                ExperimentType = g.Key.ExperimentType,
                                TimeLabel = g.Key.TimeLabel,
                                Start = g.Key.SessionStart,
                                End = g.Key.SessionEnd,
                                Notes = g.Key.Notes,
                                EegDataCount = g.Count(),
                                EegData = g.Select(r => r.EegData).ToList()
                            }).ToList()
                        });
                    }
                }

                if (!allData.Any())
                {
                    throw new InvalidOperationException("Secilen filtreler icin EEG verisi bulunamadi.");
                }

                var exportData = new
                {
                    ExportDate = DateTime.Now,
                    Users = allData
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(exportData, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
            }
        }

        private static string NormalizeKey(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                return string.Empty;
            }

            return label.Trim();
        }

        private static string BuildDisplayLabel(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? "Etiketsiz" : key;
        }

        private static string BuildSheetName(string baseName, HashSet<string> used)
        {
            var sanitized = new string(baseName.Where(ch => !InvalidSheetChars.Contains(ch)).ToArray()).Trim();
            if (string.IsNullOrEmpty(sanitized))
            {
                sanitized = "Sheet";
            }

            const int maxLength = 31;
            if (sanitized.Length > maxLength)
            {
                sanitized = sanitized.Substring(0, maxLength);
            }

            var candidate = sanitized;
            var counter = 1;
            while (!used.Add(candidate))
            {
                counter++;
                var suffix = " (" + counter.ToString(CultureInfo.InvariantCulture) + ")";
                var baseLength = Math.Min(maxLength - suffix.Length, sanitized.Length);
                candidate = sanitized.Substring(0, baseLength) + suffix;
            }

            return candidate;
        }

        private static Row CreateRow(params Cell[] cells)
        {
            var row = new Row();
            foreach (var cell in cells)
            {
                row.Append(cell);
            }

            return row;
        }

        private static Cell CreateTextCell(string text, uint? styleIndex = null)
        {
            var cell = new Cell
            {
                DataType = CellValues.String,
                CellValue = new CellValue(text ?? string.Empty)
            };
            if (styleIndex.HasValue)
            {
                cell.StyleIndex = styleIndex.Value;
            }
            return cell;
        }

        private static Cell CreateNumberCell(double? value, uint? styleIndex = null)
        {
            if (!value.HasValue)
            {
                return new Cell { StyleIndex = styleIndex ?? 0 };
            }

            var cell = new Cell
            {
                DataType = CellValues.Number,
                CellValue = new CellValue(value.Value.ToString(CultureInfo.InvariantCulture))
            };
            if (styleIndex.HasValue)
            {
                cell.StyleIndex = styleIndex.Value;
            }
            return cell;
        }

        private static Cell CreateIntegerCell(int? value, uint? styleIndex = null)
        {
            if (!value.HasValue)
            {
                return new Cell { StyleIndex = styleIndex ?? 0 };
            }

            var cell = new Cell
            {
                DataType = CellValues.Number,
                CellValue = new CellValue(value.Value.ToString(CultureInfo.InvariantCulture))
            };
            if (styleIndex.HasValue)
            {
                cell.StyleIndex = styleIndex.Value;
            }
            return cell;
        }

        private sealed class ExportRow
        {
            public int SessionId { get; set; }

            public string TimeLabel { get; set; }

            public string ExperimentType { get; set; }

            public double? Delta { get; set; }

            public double? Theta { get; set; }

            public double? LowAlpha { get; set; }

            public double? HighAlpha { get; set; }

            public double? LowBeta { get; set; }

            public double? HighBeta { get; set; }

            public double? LowGamma { get; set; }

            public double? HighGamma { get; set; }

            public int? BlinkStrength { get; set; }

            public DateTime TimestampUtc { get; set; }
        }
    }
}
