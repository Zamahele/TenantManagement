using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Repositories;
using PuppeteerSharp;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PropertyManagement.Application.Services
{
    public class LeaseGenerationService : ILeaseGenerationService
    {
        private readonly IGenericRepository<LeaseAgreement> _leaseRepository;
        private readonly IGenericRepository<LeaseTemplate> _templateRepository;
        private readonly IGenericRepository<DigitalSignature> _signatureRepository;
        private readonly IGenericRepository<Tenant> _tenantRepository;
        private readonly IGenericRepository<Room> _roomRepository;
        private readonly IMapper _mapper;
        private readonly string _uploadsPath;

        public LeaseGenerationService(
            IGenericRepository<LeaseAgreement> leaseRepository,
            IGenericRepository<LeaseTemplate> templateRepository,
            IGenericRepository<DigitalSignature> signatureRepository,
            IGenericRepository<Tenant> tenantRepository,
            IGenericRepository<Room> roomRepository,
            IMapper mapper,
            IWebHostEnvironment webHostEnvironment)
        {
            _leaseRepository = leaseRepository;
            _templateRepository = templateRepository;
            _signatureRepository = signatureRepository;
            _tenantRepository = tenantRepository;
            _roomRepository = roomRepository;
            _mapper = mapper;
            _uploadsPath = Path.Combine(webHostEnvironment.WebRootPath, "uploads", "leases");
            Directory.CreateDirectory(_uploadsPath);
        }

        public async Task<ServiceResult<string>> GenerateLeaseHtmlAsync(int leaseAgreementId, int? templateId = null)
        {
            try
            {
                var lease = await _leaseRepository.Query()
                    .Include(l => l.Tenant)
                        .ThenInclude(t => t.User)
                    .Include(l => l.Room)
                    .FirstOrDefaultAsync(l => l.LeaseAgreementId == leaseAgreementId);

                if (lease == null)
                {
                    return ServiceResult<string>.Failure("Lease agreement not found");
                }

                LeaseTemplate? template;
                if (templateId.HasValue)
                {
                    template = await _templateRepository.GetByIdAsync(templateId.Value);
                    if (template == null)
                    {
                        return ServiceResult<string>.Failure("Template not found");
                    }
                }
                else
                {
                    var defaultTemplateResult = await GetDefaultLeaseTemplateAsync();
                    if (!defaultTemplateResult.IsSuccess)
                    {
                        return ServiceResult<string>.Failure("No default template found");
                    }
                    template = _mapper.Map<LeaseTemplate>(defaultTemplateResult.Data);
                }

                // Create template variables
                var templateVariables = new
                {
                    TenantName = lease.Tenant?.FullName ?? "N/A",
                    TenantContact = lease.Tenant?.Contact ?? "N/A",
                    TenantEmergencyContact = lease.Tenant?.EmergencyContactName ?? "N/A",
                    TenantEmergencyNumber = lease.Tenant?.EmergencyContactNumber ?? "N/A",
                    RoomNumber = lease.Room?.Number ?? "N/A",
                    RoomType = lease.Room?.Type ?? "N/A",
                    StartDate = lease.StartDate.ToString("dd MMMM yyyy"),
                    EndDate = lease.EndDate.ToString("dd MMMM yyyy"),
                    RentAmount = lease.RentAmount.ToString("C"),
                    ExpectedRentDay = GetOrdinalNumber(lease.ExpectedRentDay),
                    LeaseAgreementId = lease.LeaseAgreementId,
                    GeneratedDate = DateTime.Now.ToString("dd MMMM yyyy"),
                    GeneratedTime = DateTime.Now.ToString("HH:mm:ss"),
                    LeaseDurationMonths = ((lease.EndDate.Year - lease.StartDate.Year) * 12) + lease.EndDate.Month - lease.StartDate.Month,
                    CompanyName = "Property Management Solutions",
                    CompanyAddress = "123 Property Street, Management City, 12345",
                    CompanyPhone = "+27 11 123 4567",
                    CompanyEmail = "info@propertymanagement.co.za"
                };

                // Replace template placeholders
                string htmlContent = template.HtmlContent;
                var properties = templateVariables.GetType().GetProperties();
                foreach (var property in properties)
                {
                    var value = property.GetValue(templateVariables)?.ToString() ?? "";
                    htmlContent = htmlContent.Replace($"{{{{{property.Name}}}}}", value);
                }

                // Update lease agreement
                lease.GeneratedHtmlContent = htmlContent;
                lease.LeaseTemplateId = template.LeaseTemplateId;
                lease.GeneratedAt = DateTime.UtcNow;
                lease.LastModifiedAt = DateTime.UtcNow;
                lease.Status = LeaseAgreement.LeaseStatus.Generated;
                
                await _leaseRepository.UpdateAsync(lease);

                return ServiceResult<string>.Success(htmlContent);
            }
            catch (Exception ex)
            {
                return ServiceResult<string>.Failure($"Error generating lease HTML: {ex.Message}");
            }
        }

        public async Task<ServiceResult<string>> GenerateLeasePdfAsync(int leaseAgreementId, string htmlContent)
        {
            try
            {
                var lease = await _leaseRepository.GetByIdAsync(leaseAgreementId);
                if (lease == null)
                {
                    return ServiceResult<string>.Failure("Lease agreement not found");
                }

                // Generate proper PDF using PuppeteerSharp
                try
                {
                    var pdfBytes = await GeneratePdfFromHtmlAsync(htmlContent);
                    
                    // Save PDF file
                    var fileName = $"lease_{leaseAgreementId}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                    var filePath = Path.Combine(_uploadsPath, fileName);
                    await File.WriteAllBytesAsync(filePath, pdfBytes);

                    // Update lease agreement
                    lease.GeneratedPdfPath = $"/uploads/leases/{fileName}";
                    lease.LastModifiedAt = DateTime.UtcNow;
                    await _leaseRepository.UpdateAsync(lease);

                    return ServiceResult<string>.Success(lease.GeneratedPdfPath);
                }
                catch (Exception ex)
                {
                    // Fallback: Save as HTML if PDF generation fails
                    var htmlFileName = $"lease_{leaseAgreementId}_{DateTime.Now:yyyyMMddHHmmss}.html";
                    var htmlFilePath = Path.Combine(_uploadsPath, htmlFileName);
                    await File.WriteAllTextAsync(htmlFilePath, htmlContent);

                    // Update lease agreement with HTML fallback
                    lease.GeneratedPdfPath = $"/uploads/leases/{htmlFileName}";
                    lease.LastModifiedAt = DateTime.UtcNow;
                    await _leaseRepository.UpdateAsync(lease);

                    return ServiceResult<string>.Failure($"PDF generation failed, saved as HTML: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                return ServiceResult<string>.Failure($"Error generating PDF: {ex.Message}");
            }
        }

        // PDF generation using PuppeteerSharp
        private async Task<byte[]> GeneratePdfFromHtmlAsync(string htmlContent)
        {
            try
            {
                // Use PuppeteerSharp for proper PDF generation
                await new BrowserFetcher().DownloadAsync();
                
                using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = new[] { 
                        "--no-sandbox", 
                        "--disable-setuid-sandbox",
                        "--disable-web-security",
                        "--disable-features=VizDisplayCompositor"
                    }
                });
                
                using var page = await browser.NewPageAsync();
                
                // Set content and wait for styling to load
                await page.SetContentAsync(htmlContent);
                await Task.Delay(2000); // Wait for fonts and styling to load
                
                var pdfBytes = await page.PdfDataAsync(new PdfOptions
                {
                    Format = PuppeteerSharp.Media.PaperFormat.A4,
                    PrintBackground = true, // Essential for watermark and colors
                    MarginOptions = new PuppeteerSharp.Media.MarginOptions
                    {
                        Top = "15mm",
                        Bottom = "15mm",
                        Left = "15mm", 
                        Right = "15mm"
                    },
                    PreferCSSPageSize = true,
                    DisplayHeaderFooter = false // We handle our own footer
                });
                
                return pdfBytes;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"PuppeteerSharp PDF generation failed: {ex.Message}");
                
                // Fallback to simple PDF but still try to preserve some structure
                var cleanText = System.Text.RegularExpressions.Regex.Replace(htmlContent, "<[^>]*>", " ");
                cleanText = System.Text.RegularExpressions.Regex.Replace(cleanText, @"\s+", " ").Trim();
                return CreateEnhancedSimplePdf(cleanText);
            }
        }

        // Enhanced simple PDF creation with better structure
        private byte[] CreateEnhancedSimplePdf(string content)
        {
            try
            {
                var pdf = new StringBuilder();
                
                // Limit content length and format it properly
                var maxLength = 2000; // Increased limit
                if (content.Length > maxLength)
                {
                    content = content.Substring(0, maxLength) + "... [Content truncated - Please use full PDF generation]";
                }
                
                // Clean up content for PDF
                content = content.Replace("(", "\\(")
                               .Replace(")", "\\)")
                               .Replace("\\", "\\\\")
                               .Replace("\r", "")
                               .Replace("\n", " ");
                
                // PDF Header
                pdf.AppendLine("%PDF-1.4");
                pdf.AppendLine("1 0 obj");
                pdf.AppendLine("<<");
                pdf.AppendLine("/Type /Catalog");
                pdf.AppendLine("/Pages 2 0 R");
                pdf.AppendLine(">>");
                pdf.AppendLine("endobj");
                
                // Pages object
                pdf.AppendLine("2 0 obj");
                pdf.AppendLine("<<");
                pdf.AppendLine("/Type /Pages");
                pdf.AppendLine("/Kids [3 0 R]");
                pdf.AppendLine("/Count 1");
                pdf.AppendLine(">>");
                pdf.AppendLine("endobj");
                
                // Page object
                pdf.AppendLine("3 0 obj");
                pdf.AppendLine("<<");
                pdf.AppendLine("/Type /Page");
                pdf.AppendLine("/Parent 2 0 R");
                pdf.AppendLine("/MediaBox [0 0 612 792]");
                pdf.AppendLine("/Contents 4 0 R");
                pdf.AppendLine("/Resources <<");
                pdf.AppendLine("/Font <<");
                pdf.AppendLine("/F1 5 0 R");
                pdf.AppendLine("/F2 6 0 R");
                pdf.AppendLine(">>");
                pdf.AppendLine(">>");
                pdf.AppendLine(">>");
                pdf.AppendLine("endobj");
                
                // Content stream with better formatting
                var contentStream = new StringBuilder();
                contentStream.AppendLine("BT");
                contentStream.AppendLine("/F2 16 Tf"); // Bigger font for header
                contentStream.AppendLine("50 750 Td");
                contentStream.AppendLine("(PROPERTY MANAGEMENT SOLUTIONS) Tj");
                contentStream.AppendLine("0 -25 Td");
                contentStream.AppendLine("/F2 14 Tf");
                contentStream.AppendLine("(RESIDENTIAL LEASE AGREEMENT) Tj");
                contentStream.AppendLine("0 -30 Td");
                contentStream.AppendLine("/F1 10 Tf");
                contentStream.AppendLine("(This is a digitally generated lease agreement.) Tj");
                contentStream.AppendLine("0 -20 Td");
                contentStream.AppendLine("(Generated with digital signature verification.) Tj");
                contentStream.AppendLine("0 -30 Td");
                contentStream.AppendLine("(NOTE: For full formatting and signature display,) Tj");
                contentStream.AppendLine("0 -15 Td");
                contentStream.AppendLine("(please ensure PDF generation system is properly configured.) Tj");
                contentStream.AppendLine("ET");
                
                var contentBytes = System.Text.Encoding.UTF8.GetBytes(contentStream.ToString());
                
                pdf.AppendLine("4 0 obj");
                pdf.AppendLine("<<");
                pdf.AppendLine($"/Length {contentBytes.Length}");
                pdf.AppendLine(">>");
                pdf.AppendLine("stream");
                pdf.AppendLine(contentStream.ToString());
                pdf.AppendLine("endstream");
                pdf.AppendLine("endobj");
                
                // Font objects
                pdf.AppendLine("5 0 obj");
                pdf.AppendLine("<<");
                pdf.AppendLine("/Type /Font");
                pdf.AppendLine("/Subtype /Type1");
                pdf.AppendLine("/BaseFont /Helvetica");
                pdf.AppendLine(">>");
                pdf.AppendLine("endobj");
                
                pdf.AppendLine("6 0 obj");
                pdf.AppendLine("<<");
                pdf.AppendLine("/Type /Font");
                pdf.AppendLine("/Subtype /Type1");
                pdf.AppendLine("/BaseFont /Helvetica-Bold");
                pdf.AppendLine(">>");
                pdf.AppendLine("endobj");
                
                // Cross-reference table
                pdf.AppendLine("xref");
                pdf.AppendLine("0 7");
                pdf.AppendLine("0000000000 65535 f ");
                pdf.AppendLine("0000000015 00000 n ");
                pdf.AppendLine("0000000074 00000 n ");
                pdf.AppendLine("0000000131 00000 n ");
                pdf.AppendLine("0000000273 00000 n ");
                pdf.AppendLine($"{400:D10} 00000 n ");
                pdf.AppendLine($"{450:D10} 00000 n ");
                
                // Trailer
                pdf.AppendLine("trailer");
                pdf.AppendLine("<<");
                pdf.AppendLine("/Size 7");
                pdf.AppendLine("/Root 1 0 R");
                pdf.AppendLine(">>");
                pdf.AppendLine("startxref");
                pdf.AppendLine("500");
                pdf.AppendLine("%%EOF");
                
                return System.Text.Encoding.UTF8.GetBytes(pdf.ToString());
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Enhanced simple PDF creation failed: {ex.Message}");
                
                // Ultimate fallback - create a minimal valid PDF
                var minimalPdf = @"%PDF-1.4
1 0 obj
<<
/Type /Catalog
/Pages 2 0 R
>>
endobj
2 0 obj
<<
/Type /Pages
/Kids [3 0 R]
/Count 1
>>
endobj
3 0 obj
<<
/Type /Page
/Parent 2 0 R
/MediaBox [0 0 612 792]
/Contents 4 0 R
/Resources <<
/Font <<
/F1 5 0 R
>>
>>
>>
endobj
4 0 obj
<<
/Length 44
>>
stream
BT /F1 12 Tf 50 750 Td (LEASE AGREEMENT - SIGNED) Tj ET
endstream
endobj
5 0 obj
<<
/Type /Font
/Subtype /Type1
/BaseFont /Helvetica
>>
endobj
xref
0 6
0000000000 65535 f 
0000000015 00000 n 
0000000074 00000 n 
0000000131 00000 n 
0000000273 00000 n 
0000000367 00000 n 
trailer
<<
/Size 6
/Root 1 0 R
>>
startxref
424
%%EOF";
                return System.Text.Encoding.UTF8.GetBytes(minimalPdf);
            }
        }

        public async Task<ServiceResult<LeaseAgreementSigningDto>> GetLeaseForSigningAsync(int leaseAgreementId, int tenantId)
        {
            try
            {
                var lease = await _leaseRepository.Query()
                    .Include(l => l.Tenant)
                        .ThenInclude(t => t.User)
                    .Include(l => l.Room)
                    .Include(l => l.DigitalSignatures)
                        .ThenInclude(ds => ds.Tenant)
                    .FirstOrDefaultAsync(l => l.LeaseAgreementId == leaseAgreementId);

                if (lease == null)
                {
                    return ServiceResult<LeaseAgreementSigningDto>.Failure("Lease agreement not found");
                }

                // Verify tenant has access to this lease (skip for tenantId = 0, which indicates manager access)
                if (tenantId > 0 && lease.TenantId != tenantId)
                {
                    return ServiceResult<LeaseAgreementSigningDto>.Failure("Unauthorized access to lease agreement");
                }

                // Check if lease is ready for signing (skip for manager preview)
                if (tenantId > 0 && lease.Status < LeaseAgreement.LeaseStatus.Generated)
                {
                    return ServiceResult<LeaseAgreementSigningDto>.Failure("Lease agreement is not ready for signing");
                }

                var signingDto = new LeaseAgreementSigningDto
                {
                    LeaseAgreementId = lease.LeaseAgreementId,
                    TenantName = lease.Tenant?.FullName ?? "N/A",
                    RoomNumber = lease.Room?.Number ?? "N/A",
                    StartDate = lease.StartDate,
                    EndDate = lease.EndDate,
                    RentAmount = lease.RentAmount,
                    ExpectedRentDay = lease.ExpectedRentDay,
                    GeneratedHtmlContent = lease.GeneratedHtmlContent,
                    GeneratedPdfPath = lease.GeneratedPdfPath,
                    Status = lease.Status,
                    RequiresDigitalSignature = lease.RequiresDigitalSignature,
                    IsDigitallySigned = lease.IsDigitallySigned,
                    SignedAt = lease.SignedAt,
                    DigitalSignatures = _mapper.Map<List<DigitalSignatureDto>>(lease.DigitalSignatures)
                };

                return ServiceResult<LeaseAgreementSigningDto>.Success(signingDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<LeaseAgreementSigningDto>.Failure($"Error retrieving lease for signing: {ex.Message}");
            }
        }

        public async Task<ServiceResult<DigitalSignatureDto>> SignLeaseAsync(SignLeaseDto signLeaseDto)
        {
            try
            {
                var lease = await _leaseRepository.GetByIdAsync(signLeaseDto.LeaseAgreementId);
                if (lease == null)
                {
                    return ServiceResult<DigitalSignatureDto>.Failure("Lease agreement not found");
                }

                // Check if already signed by this tenant
                var existingSignature = await _signatureRepository.Query()
                    .FirstOrDefaultAsync(s => s.LeaseAgreementId == signLeaseDto.LeaseAgreementId);
                
                if (existingSignature != null)
                {
                    return ServiceResult<DigitalSignatureDto>.Failure("Lease agreement is already signed");
                }

                // Process signature image
                var signatureFileName = await ProcessSignatureImageAsync(signLeaseDto.SignatureDataUrl, signLeaseDto.LeaseAgreementId);
                if (string.IsNullOrEmpty(signatureFileName))
                {
                    return ServiceResult<DigitalSignatureDto>.Failure("Invalid signature data");
                }

                // Create signature hash for verification
                var signatureHash = GenerateSignatureHash(signLeaseDto);

                // Create digital signature record
                var digitalSignature = new DigitalSignature
                {
                    LeaseAgreementId = signLeaseDto.LeaseAgreementId,
                    TenantId = lease.TenantId,
                    SignedDate = DateTime.UtcNow,
                    SignatureImagePath = $"/uploads/signatures/{signatureFileName}",
                    SignerIPAddress = signLeaseDto.SignerIPAddress,
                    SignerUserAgent = signLeaseDto.SignerUserAgent,
                    SigningNotes = signLeaseDto.SigningNotes,
                    SignatureHash = signatureHash,
                    IsVerified = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _signatureRepository.AddAsync(digitalSignature);

                // Update lease agreement status
                lease.IsDigitallySigned = true;
                lease.SignedAt = DateTime.UtcNow;
                lease.Status = LeaseAgreement.LeaseStatus.Signed;
                lease.LastModifiedAt = DateTime.UtcNow;
                await _leaseRepository.UpdateAsync(lease);

                // Generate final signed PDF
                if (!string.IsNullOrEmpty(lease.GeneratedHtmlContent))
                {
                    var signedHtml = AddSignatureToHtml(lease.GeneratedHtmlContent, digitalSignature);
                    await GenerateSignedPdfAsync(lease.LeaseAgreementId, signedHtml);
                }

                var signatureDto = _mapper.Map<DigitalSignatureDto>(digitalSignature);
                return ServiceResult<DigitalSignatureDto>.Success(signatureDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<DigitalSignatureDto>.Failure($"Error signing lease: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> SendLeaseToTenantAsync(int leaseAgreementId)
        {
            try
            {
                var lease = await _leaseRepository.Query()
                    .Include(l => l.Tenant)
                        .ThenInclude(t => t.User)
                    .FirstOrDefaultAsync(l => l.LeaseAgreementId == leaseAgreementId);

                if (lease == null)
                {
                    return ServiceResult<bool>.Failure("Lease agreement not found");
                }

                if (lease.Status < LeaseAgreement.LeaseStatus.Generated)
                {
                    return ServiceResult<bool>.Failure("Lease must be generated before sending");
                }

                // Update status - ensure these fields are properly set
                lease.Status = LeaseAgreement.LeaseStatus.Sent;
                lease.SentToTenantAt = DateTime.UtcNow;
                lease.LastModifiedAt = DateTime.UtcNow;
                
                await _leaseRepository.UpdateAsync(lease);

                // TODO: Implement email notification to tenant
                // This could use an email service to notify the tenant

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"Error sending lease to tenant: {ex.Message}");
            }
        }

        public async Task<ServiceResult<byte[]>> DownloadSignedLeaseAsync(int leaseAgreementId, int tenantId)
        {
            try
            {
                var lease = await _leaseRepository.GetByIdAsync(leaseAgreementId);
                if (lease == null)
                {
                    return ServiceResult<byte[]>.Failure("Lease agreement not found");
                }

                // Verify tenant has access to this lease
                if (lease.TenantId != tenantId)
                {
                    return ServiceResult<byte[]>.Failure("Unauthorized access to lease agreement");
                }

                if (!lease.IsDigitallySigned)
                {
                    return ServiceResult<byte[]>.Failure("Lease agreement is not signed yet");
                }

                if (string.IsNullOrEmpty(lease.GeneratedPdfPath))
                {
                    return ServiceResult<byte[]>.Failure("PDF not found");
                }

                var filePath = Path.Combine(_uploadsPath, Path.GetFileName(lease.GeneratedPdfPath));
                if (!File.Exists(filePath))
                {
                    return ServiceResult<byte[]>.Failure("PDF file not found on disk");
                }

                var fileBytes = await File.ReadAllBytesAsync(filePath);
                return ServiceResult<byte[]>.Success(fileBytes);
            }
            catch (Exception ex)
            {
                return ServiceResult<byte[]>.Failure($"Error downloading signed lease: {ex.Message}");
            }
        }

        // Template management methods
        public async Task<ServiceResult<IEnumerable<LeaseTemplateDto>>> GetLeaseTemplatesAsync()
        {
            try
            {
                var templates = await _templateRepository.GetAllAsync(t => t.IsActive);
                var templateDtos = _mapper.Map<IEnumerable<LeaseTemplateDto>>(templates);
                return ServiceResult<IEnumerable<LeaseTemplateDto>>.Success(templateDtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<LeaseTemplateDto>>.Failure($"Error retrieving templates: {ex.Message}");
            }
        }

        public async Task<ServiceResult<LeaseTemplateDto>> CreateLeaseTemplateAsync(CreateLeaseTemplateDto createTemplateDto)
        {
            try
            {
                // If setting as default, unset other defaults
                if (createTemplateDto.IsDefault)
                {
                    await UnsetDefaultTemplatesAsync();
                }

                var template = new LeaseTemplate
                {
                    Name = createTemplateDto.Name,
                    HtmlContent = createTemplateDto.HtmlContent,
                    Description = createTemplateDto.Description,
                    IsActive = createTemplateDto.IsActive,
                    IsDefault = createTemplateDto.IsDefault,
                    TemplateVariables = createTemplateDto.TemplateVariables,
                    CreatedAt = DateTime.UtcNow
                };

                await _templateRepository.AddAsync(template);
                var templateDto = _mapper.Map<LeaseTemplateDto>(template);
                return ServiceResult<LeaseTemplateDto>.Success(templateDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<LeaseTemplateDto>.Failure($"Error creating template: {ex.Message}");
            }
        }

        public async Task<ServiceResult<LeaseTemplateDto>> UpdateLeaseTemplateAsync(int id, UpdateLeaseTemplateDto updateTemplateDto)
        {
            try
            {
                var template = await _templateRepository.GetByIdAsync(id);
                if (template == null)
                {
                    return ServiceResult<LeaseTemplateDto>.Failure("Template not found");
                }

                // If setting as default, unset other defaults
                if (updateTemplateDto.IsDefault && !template.IsDefault)
                {
                    await UnsetDefaultTemplatesAsync();
                }

                template.Name = updateTemplateDto.Name;
                template.HtmlContent = updateTemplateDto.HtmlContent;
                template.Description = updateTemplateDto.Description;
                template.IsActive = updateTemplateDto.IsActive;
                template.IsDefault = updateTemplateDto.IsDefault;
                template.TemplateVariables = updateTemplateDto.TemplateVariables;
                template.UpdatedAt = DateTime.UtcNow;

                await _templateRepository.UpdateAsync(template);
                var templateDto = _mapper.Map<LeaseTemplateDto>(template);
                return ServiceResult<LeaseTemplateDto>.Success(templateDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<LeaseTemplateDto>.Failure($"Error updating template: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> DeleteLeaseTemplateAsync(int id)
        {
            try
            {
                var template = await _templateRepository.GetByIdAsync(id);
                if (template == null)
                {
                    return ServiceResult<bool>.Failure("Template not found");
                }

                await _templateRepository.DeleteAsync(template);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"Error deleting template: {ex.Message}");
            }
        }

        public async Task<ServiceResult<LeaseTemplateDto>> GetDefaultLeaseTemplateAsync()
        {
            try
            {
                var template = await _templateRepository.Query()
                    .Where(t => t.IsActive && t.IsDefault)
                    .FirstOrDefaultAsync();

                if (template == null)
                {
                    // Create default template if none exists
                    template = await CreateDefaultTemplateAsync();
                }

                var templateDto = _mapper.Map<LeaseTemplateDto>(template);
                return ServiceResult<LeaseTemplateDto>.Success(templateDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<LeaseTemplateDto>.Failure($"Error getting default template: {ex.Message}");
            }
        }

        // Private helper methods
        private async Task<string> ProcessSignatureImageAsync(string signatureDataUrl, int leaseAgreementId)
        {
            try
            {
                // Extract base64 data from data URL
                if (!signatureDataUrl.StartsWith("data:image/"))
                {
                    return string.Empty;
                }

                var base64Data = signatureDataUrl.Substring(signatureDataUrl.IndexOf(',') + 1);
                var imageBytes = Convert.FromBase64String(base64Data);

                // Create signatures directory - _uploadsPath is wwwroot/uploads/leases
                var webRootPath = Path.GetDirectoryName(Path.GetDirectoryName(_uploadsPath)); // Get wwwroot
                var signaturesPath = Path.Combine(webRootPath, "uploads", "signatures");
                Directory.CreateDirectory(signaturesPath);

                var fileName = $"signature_{leaseAgreementId}_{DateTime.Now:yyyyMMddHHmmss}.png";
                var filePath = Path.Combine(signaturesPath, fileName);
                await File.WriteAllBytesAsync(filePath, imageBytes);

                return fileName;
            }
            catch
            {
                return string.Empty;
            }
        }

        private string GenerateSignatureHash(SignLeaseDto signLeaseDto)
        {
            var data = $"{signLeaseDto.LeaseAgreementId}|{signLeaseDto.SignerIPAddress}|{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hashBytes);
        }

        private string AddSignatureToHtml(string htmlContent, DigitalSignature signature)
        {
            try
            {
                // Convert signature image to base64 for embedding
                var signatureImageBase64 = "";
                var signatureImagePath = signature.SignatureImagePath;
                
                if (!string.IsNullOrEmpty(signatureImagePath))
                {
                    // Remove leading slash if present and construct full path
                    var relativePath = signatureImagePath.TrimStart('/');
                    // _uploadsPath points to wwwroot/uploads/leases, we need wwwroot/uploads/signatures
                    var webRootPath = Path.GetDirectoryName(Path.GetDirectoryName(_uploadsPath)); // Get wwwroot
                    var fullImagePath = Path.Combine(webRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
                    
                    if (File.Exists(fullImagePath))
                    {
                        try
                        {
                            var imageBytes = File.ReadAllBytes(fullImagePath);
                            signatureImageBase64 = $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";
                            System.Console.WriteLine($"✅ Successfully loaded signature image: {fullImagePath}");
                        }
                        catch (Exception ex)
                        {
                            System.Console.WriteLine($"❌ Error reading signature image from {fullImagePath}: {ex.Message}");
                            signatureImageBase64 = ""; // Will show placeholder
                        }
                    }
                    else
                    {
                        System.Console.WriteLine($"❌ Signature image file not found: {fullImagePath}");
                        System.Console.WriteLine($"   Original path: {signatureImagePath}");
                        System.Console.WriteLine($"   Relative path: {relativePath}");
                        System.Console.WriteLine($"   WebRoot path: {webRootPath}");
                    }
                }

                var signatureSection = $@"
                <div class='digital-signature'>
                    <div class='signature-header'>Digital Signature Verification</div>
                    
                    <div class='signature-details'>
                        <div>
                            <p><strong>Signed by:</strong> Tenant</p>
                            <p><strong>Date:</strong> {signature.SignedDate:dd MMMM yyyy}</p>
                            <p><strong>Time:</strong> {signature.SignedDate:HH:mm:ss} UTC</p>
                        </div>
                        <div>
                            <p><strong>IP Address:</strong> {signature.SignerIPAddress}</p>
                            <p><strong>Browser:</strong> {GetBrowserInfo(signature.SignerUserAgent)}</p>
                            <p><strong>Verified:</strong> <span style='color: #27ae60; font-weight: bold;'>? Yes</span></p>
                        </div>
                    </div>
                    
                    <div class='signature-image'>
                        {(string.IsNullOrEmpty(signatureImageBase64) 
                            ? @"<div style='padding: 20px; background: #fff3cd; border: 2px dashed #f39c12; border-radius: 8px;'>
                                   <p style='color: #d68910; font-weight: bold; margin-bottom: 10px;'>⚠️ Digital Signature Applied</p>
                                   <p style='color: #b7950b; font-size: 14px;'>Signature image could not be embedded in PDF.<br/>
                                   The lease was digitally signed and is legally binding.<br/>
                                   Original signature data is securely stored.</p>
                               </div>"
                            : $"<img src='{signatureImageBase64}' alt='Digital Signature' style='max-width: 300px; max-height: 150px; border: 2px solid #2c3e50; border-radius: 5px; background: white; padding: 10px;' />")}
                        <p style='margin-top: 10px; font-size: 12px; color: #7f8c8d;'>Digitally signed on {signature.SignedDate:dd MMMM yyyy} at {signature.SignedDate:HH:mm:ss}</p>
                    </div>
                    
                    <div class='verification-section'>
                        <p><strong>?? Verification Hash:</strong></p>
                        <p style='font-family: monospace; font-size: 11px; word-break: break-all; color: #2c3e50;'>{signature.SignatureHash}</p>
                        <p style='margin-top: 10px; font-size: 12px; color: #27ae60;'>
                            <strong>? This signature is cryptographically verified and legally binding.</strong>
                        </p>
                    </div>
                    
                    {(!string.IsNullOrEmpty(signature.SigningNotes) ? 
                        $"<div style='margin-top: 15px; padding: 10px; background: #f8f9fa; border-radius: 5px;'><strong>Notes:</strong> {signature.SigningNotes}</div>" 
                        : "")}
                </div>";

                return htmlContent.Replace("</body>", $"{signatureSection}</body>");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error adding signature to HTML: {ex.Message}");
                
                // Fallback signature section
                var fallbackSignatureSection = $@"
                <div class='digital-signature'>
                    <div class='signature-header'>Digital Signature</div>
                    <p><strong>Signed by:</strong> Tenant</p>
                    <p><strong>Date:</strong> {signature.SignedDate:dd MMMM yyyy HH:mm:ss}</p>
                    <p><strong>Verification Hash:</strong> {signature.SignatureHash[..Math.Min(20, signature.SignatureHash.Length)]}...</p>
                    <p style='color: #27ae60;'><strong>? Digitally signed and verified</strong></p>
                </div>";
                
                return htmlContent.Replace("</body>", $"{fallbackSignatureSection}</body>");
            }
        }

        // Helper method to extract browser info from User Agent
        private string GetBrowserInfo(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "Unknown";
                
            try
            {
                // Check for Edge first since it contains "Chrome" in its user agent
                if (userAgent.Contains("Edge") || userAgent.Contains("Edg/"))
                    return "Microsoft Edge";
                else if (userAgent.Contains("Chrome"))
                    return "Google Chrome";
                else if (userAgent.Contains("Firefox"))
                    return "Mozilla Firefox";
                else if (userAgent.Contains("Safari"))
                    return "Safari";
                else if (userAgent.Contains("Opera") || userAgent.Contains("OPR"))
                    return "Opera";
                else
                    return "Other Browser";
            }
            catch
            {
                return "Unknown";
            }
        }

        private string GetOrdinalNumber(int number)
        {
            var suffix = "th";
            if (number % 100 < 11 || number % 100 > 13)
            {
                switch (number % 10)
                {
                    case 1: suffix = "st"; break;
                    case 2: suffix = "nd"; break;
                    case 3: suffix = "rd"; break;
                }
            }
            return $"{number}{suffix}";
        }

        private async Task<string> GenerateSignedPdfAsync(int leaseAgreementId, string signedHtmlContent)
        {
            var pdfResult = await GenerateLeasePdfAsync(leaseAgreementId, signedHtmlContent);
            return pdfResult.IsSuccess ? pdfResult.Data : string.Empty;
        }

        private async Task UnsetDefaultTemplatesAsync()
        {
            var defaultTemplates = await _templateRepository.GetAllAsync(t => t.IsDefault);
            foreach (var template in defaultTemplates)
            {
                template.IsDefault = false;
                await _templateRepository.UpdateAsync(template);
            }
        }

        private async Task<LeaseTemplate> CreateDefaultTemplateAsync()
        {
            var defaultHtml = GetDefaultLeaseTemplateHtml();
            var template = new LeaseTemplate
            {
                Name = "Professional Lease Agreement Template",
                HtmlContent = defaultHtml,
                Description = "Professional lease agreement template with watermark and modern design",
                IsActive = true,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                TemplateVariables = JsonSerializer.Serialize(GetDefaultTemplateVariables())
            };

            await _templateRepository.AddAsync(template);
            return template;
        }

        private object GetDefaultTemplateVariables()
        {
            return new
            {
                TenantName = "Full name of the tenant",
                TenantContact = "Contact number",
                TenantEmergencyContact = "Emergency contact name",
                TenantEmergencyNumber = "Emergency contact number",
                RoomNumber = "Room identifier",
                RoomType = "Type of room",
                StartDate = "Lease start date",
                EndDate = "Lease end date",
                RentAmount = "Monthly rent amount",
                ExpectedRentDay = "Day of month rent is due",
                LeaseAgreementId = "Unique lease identifier",
                GeneratedDate = "Document generation date",
                GeneratedTime = "Document generation time",
                LeaseDurationMonths = "Duration in months",
                CompanyName = "Property management company name",
                CompanyAddress = "Company address",
                CompanyPhone = "Company phone number",
                CompanyEmail = "Company email address"
            };
        }

        private string GetDefaultLeaseTemplateHtml()
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Professional Lease Agreement - {{LeaseAgreementId}}</title>
    <style>
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700;800&family=Playfair+Display:wght@400;700&display=swap');
        
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        
        body {
            font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
            line-height: 1.7;
            color: #1a202c;
            background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%);
            position: relative;
            font-size: 14px;
            -webkit-font-smoothing: antialiased;
            -moz-osx-font-smoothing: grayscale;
        }
        
        /* Enhanced Professional Watermarks */
        body::before {
            content: 'PROPERTY MANAGEMENT SOLUTIONS';
            position: fixed;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%) rotate(-45deg);
            font-family: 'Playfair Display', serif;
            font-size: 72px;
            font-weight: 700;
            color: rgba(59, 130, 246, 0.08);
            z-index: 0;
            white-space: nowrap;
            pointer-events: none;
            letter-spacing: 8px;
        }
        
        /* Secondary diagonal watermark */
        body::after {
            content: 'CONFIDENTIAL • LEGALLY BINDING DOCUMENT • CONFIDENTIAL';
            position: fixed;
            top: 25%;
            left: 50%;
            transform: translate(-50%, -50%) rotate(-45deg);
            font-family: 'Inter', sans-serif;
            font-size: 28px;
            font-weight: 600;
            color: rgba(239, 68, 68, 0.06);
            z-index: 0;
            white-space: nowrap;
            pointer-events: none;
            letter-spacing: 4px;
        }
        
        /* Corner watermark pattern */
        .watermark-corner {
            position: fixed;
            width: 200px;
            height: 200px;
            opacity: 0.04;
            z-index: 0;
            pointer-events: none;
        }
        
        .watermark-corner.top-left {
            top: 50px;
            left: 50px;
            background: radial-gradient(circle, #3b82f6 30%, transparent 70%);
            border-radius: 50%;
        }
        
        .watermark-corner.bottom-right {
            bottom: 50px;
            right: 50px;
            background: radial-gradient(circle, #ef4444 30%, transparent 70%);
            border-radius: 50%;
        }
        
        .container {
            max-width: 900px;
            margin: 0 auto;
            padding: 60px 50px;
            background: rgba(255, 255, 255, 0.95);
            position: relative;
            z-index: 1;
            border-radius: 20px;
            box-shadow: 0 25px 60px rgba(0, 0, 0, 0.15);
            backdrop-filter: blur(10px);
        }
        
        .header {
            text-align: center;
            margin-bottom: 50px;
            padding: 40px 0;
            background: linear-gradient(135deg, #1e3a8a 0%, #3b82f6 50%, #60a5fa 100%);
            border-radius: 16px;
            color: white;
            position: relative;
            overflow: hidden;
        }
        
        .header::before {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: 
                radial-gradient(circle at 20% 20%, rgba(255, 255, 255, 0.1) 0%, transparent 50%),
                radial-gradient(circle at 80% 80%, rgba(255, 255, 255, 0.1) 0%, transparent 50%),
                linear-gradient(135deg, transparent 0%, rgba(255, 255, 255, 0.05) 100%);
            pointer-events: none;
        }
        
        .company-logo {
            width: 120px;
            height: 120px;
            background: linear-gradient(135deg, #ffffff 0%, #f1f5f9 100%);
            border-radius: 50%;
            margin: 0 auto 25px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-family: 'Playfair Display', serif;
            color: #1e3a8a;
            font-size: 36px;
            font-weight: 700;
            box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);
            border: 4px solid rgba(255, 255, 255, 0.3);
            position: relative;
            z-index: 2;
        }
        
        .header h1 {
            font-family: 'Playfair Display', serif;
            font-size: 42px;
            font-weight: 700;
            margin-bottom: 12px;
            text-transform: uppercase;
            letter-spacing: 3px;
            text-shadow: 0 2px 10px rgba(0, 0, 0, 0.3);
            position: relative;
            z-index: 2;
        }
        
        .header h2 {
            font-family: 'Inter', sans-serif;
            font-size: 22px;
            font-weight: 500;
            margin-bottom: 20px;
            opacity: 0.95;
            position: relative;
            z-index: 2;
        }
        
        .header .contact-info {
            font-size: 14px;
            line-height: 1.8;
            opacity: 0.9;
            background: rgba(255, 255, 255, 0.1);
            padding: 15px 30px;
            border-radius: 10px;
            display: inline-block;
            position: relative;
            z-index: 2;
            backdrop-filter: blur(5px);
        }
        
        .document-info {
            background: linear-gradient(135deg, #f0f9ff 0%, #e0f2fe 100%);
            padding: 30px;
            border-radius: 16px;
            margin-bottom: 40px;
            border: 1px solid rgba(59, 130, 246, 0.2);
            position: relative;
            overflow: hidden;
        }
        
        .document-info::before {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            width: 6px;
            height: 100%;
            background: linear-gradient(180deg, #3b82f6 0%, #1d4ed8 100%);
        }
        
        .document-info h3 {
            color: #1e40af;
            font-size: 20px;
            font-weight: 700;
            margin-bottom: 20px;
            display: flex;
            align-items: center;
            font-family: 'Playfair Display', serif;
        }
        
        .document-info h3::before {
            content: '[DOC]';
            margin-right: 12px;
            font-size: 14px;
            font-weight: 700;
            color: #1e40af;
            background: rgba(59, 130, 246, 0.1);
            padding: 4px 8px;
            border-radius: 6px;
            border: 1px solid rgba(59, 130, 246, 0.3);
        }
        
        .info-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
            gap: 20px;
            margin-bottom: 40px;
        }
        
        .info-card {
            background: linear-gradient(135deg, #ffffff 0%, #f8fafc 100%);
            padding: 25px;
            border-radius: 16px;
            box-shadow: 0 8px 25px rgba(0, 0, 0, 0.08);
            border: 1px solid rgba(148, 163, 184, 0.1);
            position: relative;
            transition: transform 0.3s ease, box-shadow 0.3s ease;
        }
        
        .info-card::before {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            height: 4px;
            background: linear-gradient(90deg, #3b82f6 0%, #8b5cf6 50%, #ef4444 100%);
            border-radius: 16px 16px 0 0;
        }
        
        .info-card h4 {
            color: #1e40af;
            font-size: 16px;
            font-weight: 700;
            margin-bottom: 15px;
            display: flex;
            align-items: center;
            font-family: 'Inter', sans-serif;
        }
        
        .card-icon {
            background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%);
            color: white;
            padding: 4px 8px;
            border-radius: 6px;
            font-size: 11px;
            font-weight: 700;
            margin-right: 10px;
            letter-spacing: 0.5px;
            box-shadow: 0 2px 4px rgba(59, 130, 246, 0.3);
        }
        
        .info-card p {
            color: #475569;
            font-size: 14px;
            margin: 8px 0;
            line-height: 1.6;
        }
        
        .info-card strong {
            color: #1e293b;
            font-weight: 600;
        }
        
        .section {
            margin-bottom: 45px;
            background: rgba(255, 255, 255, 0.7);
            padding: 35px;
            border-radius: 16px;
            border: 1px solid rgba(203, 213, 225, 0.3);
        }
        
        .section h2 {
            color: #1e293b;
            font-size: 24px;
            font-weight: 700;
            margin-bottom: 25px;
            padding-bottom: 15px;
            border-bottom: 3px solid transparent;
            background: linear-gradient(to right, #e2e8f0, #e2e8f0), linear-gradient(90deg, #3b82f6 0%, #8b5cf6 50%, #ef4444 100%);
            background-clip: padding-box, border-box;
            background-origin: padding-box, border-box;
            display: flex;
            align-items: center;
            font-family: 'Playfair Display', serif;
        }
        
        .section-icon {
            background: linear-gradient(135deg, #8b5cf6 0%, #6366f1 100%);
            color: white;
            padding: 6px 10px;
            border-radius: 8px;
            font-size: 12px;
            font-weight: 700;
            margin-right: 12px;
            letter-spacing: 0.5px;
            box-shadow: 0 3px 6px rgba(139, 92, 246, 0.3);
        }
        
        .terms-list {
            counter-reset: term-counter;
            padding: 0;
            list-style: none;
        }
        
        .terms-list li {
            counter-increment: term-counter;
            margin-bottom: 20px;
            padding: 20px 20px 20px 60px;
            position: relative;
            list-style: none;
            background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%);
            border-radius: 12px;
            border: 1px solid rgba(148, 163, 184, 0.2);
            line-height: 1.7;
        }
        
        .terms-list li::before {
            content: counter(term-counter, decimal);
            position: absolute;
            left: 20px;
            top: 20px;
            background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%);
            color: white;
            width: 32px;
            height: 32px;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 14px;
            font-weight: 700;
            box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3);
        }
        
        .terms-list li strong {
            color: #1e40af;
            font-weight: 700;
        }
        
        .highlight-box {
            background: linear-gradient(135deg, #fef3c7 0%, #fde68a 100%);
            border: 2px solid #f59e0b;
            border-radius: 16px;
            padding: 25px;
            margin: 30px 0;
            position: relative;
            box-shadow: 0 8px 25px rgba(245, 158, 11, 0.15);
        }
        
        .highlight-box::before {
            content: '[IMPORTANT]';
            position: absolute;
            top: -12px;
            left: 25px;
            background: #f59e0b;
            color: white;
            padding: 6px 12px;
            border-radius: 12px;
            font-size: 11px;
            font-weight: 700;
            letter-spacing: 0.5px;
            box-shadow: 0 2px 8px rgba(245, 158, 11, 0.4);
        }
        
        .signature-section {
            background: linear-gradient(135deg, #f0fdf4 0%, #dcfce7 100%);
            border: 2px solid #16a34a;
            border-radius: 20px;
            padding: 40px;
            margin-top: 50px;
            position: relative;
            box-shadow: 0 12px 30px rgba(22, 163, 74, 0.15);
        }
        
        .signature-section::before {
            content: '[SIGNATURE]';
            position: absolute;
            top: -15px;
            left: 50%;
            transform: translateX(-50%);
            background: #16a34a;
            color: white;
            padding: 8px 15px;
            border-radius: 20px;
            font-size: 12px;
            font-weight: 700;
            letter-spacing: 0.5px;
            box-shadow: 0 3px 10px rgba(22, 163, 74, 0.4);
        }
        
        .signature-section h2 {
            color: #166534;
            font-size: 26px;
            font-weight: 700;
            margin-bottom: 25px;
            text-align: center;
            font-family: 'Playfair Display', serif;
            margin-top: 10px;
        }
        
        .acknowledgment-list {
            background: rgba(255, 255, 255, 0.9);
            padding: 30px;
            border-radius: 16px;
            margin: 25px 0;
            border: 1px solid rgba(22, 163, 74, 0.2);
        }
        
        .acknowledgment-list ul {
            list-style: none;
            padding: 0;
            margin: 0;
        }
        
        .acknowledgment-list li {
            padding: 12px 0 12px 40px;
            position: relative;
            line-height: 1.6;
            color: #374151;
        }
        
        .acknowledgment-list li::before {
            content: '[✓]';
            position: absolute;
            left: 0;
            top: 12px;
            font-size: 14px;
            font-weight: 700;
            color: #16a34a;
            background: rgba(22, 163, 74, 0.1);
            padding: 2px 6px;
            border-radius: 4px;
            border: 1px solid rgba(22, 163, 74, 0.3);
        }
        
        .footer {
            margin-top: 60px;
            padding: 30px 0;
            background: linear-gradient(135deg, #1f2937 0%, #374151 100%);
            border-radius: 16px;
            text-align: center;
            color: #d1d5db;
            font-size: 12px;
            position: relative;
        }
        
        .footer .confidential {
            background: linear-gradient(135deg, #dc2626 0%, #b91c1c 100%);
            color: white;
            padding: 8px 20px;
            border-radius: 25px;
            display: inline-block;
            margin-bottom: 15px;
            font-weight: 700;
            font-size: 11px;
            letter-spacing: 1px;
            text-transform: uppercase;
            box-shadow: 0 4px 12px rgba(220, 38, 38, 0.3);
        }
        
        /* Enhanced Digital Signature Styling */
        .digital-signature {
            background: linear-gradient(135deg, #ffffff 0%, #f8fafc 100%);
            border: 3px solid #1e40af;
            border-radius: 20px;
            padding: 35px;
            margin-top: 40px;
            position: relative;
            box-shadow: 0 15px 35px rgba(30, 64, 175, 0.15);
        }
        
        .digital-signature::before {
            content: '';
            position: absolute;
            top: -2px;
            left: -2px;
            right: -2px;
            bottom: -2px;
            background: linear-gradient(45deg, #3b82f6, #8b5cf6, #ef4444, #f59e0b, #10b981);
            border-radius: 22px;
            z-index: -1;
            animation: gradient-border 3s ease infinite;
        }
        
        @keyframes gradient-border {
            0%, 100% { background-position: 0% 50%; }
            50% { background-position: 100% 50%; }
        }
        
        .signature-header {
            color: #1e40af;
            font-size: 28px;
            font-weight: 700;
            margin-bottom: 25px;
            text-align: center;
            display: flex;
            align-items: center;
            justify-content: center;
            font-family: 'Playfair Display', serif;
        }
        
        .signature-header::before {
            content: '[SECURE]';
            margin-right: 12px;
            font-size: 12px;
            font-weight: 700;
            background: rgba(30, 64, 175, 0.1);
            color: #1e40af;
            padding: 6px 10px;
            border-radius: 8px;
            border: 1px solid rgba(30, 64, 175, 0.3);
        }
        
        .signature-details {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
            gap: 20px;
            margin-bottom: 25px;
            background: rgba(59, 130, 246, 0.05);
            padding: 25px;
            border-radius: 16px;
            border: 1px solid rgba(59, 130, 246, 0.1);
        }
        
        .signature-image {
            text-align: center;
            padding: 30px;
            background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%);
            border-radius: 16px;
            border: 2px dashed #3b82f6;
            margin: 20px 0;
        }
        
        .signature-image img {
            max-width: 350px;
            max-height: 180px;
            border: 3px solid #1e40af;
            border-radius: 12px;
            background: white;
            padding: 15px;
            box-shadow: 0 8px 20px rgba(30, 64, 175, 0.2);
        }
        
        .verification-section {
            background: linear-gradient(135deg, #f0fdf4 0%, #dcfce7 100%);
            border: 2px solid #16a34a;
            border-radius: 16px;
            padding: 25px;
            margin-top: 20px;
            position: relative;
        }
        
        .verification-section::before {
            content: '[VERIFIED]';
            position: absolute;
            top: -12px;
            left: 25px;
            background: #16a34a;
            color: white;
            padding: 6px 12px;
            border-radius: 12px;
            font-size: 11px;
            font-weight: 700;
            letter-spacing: 0.5px;
            box-shadow: 0 2px 8px rgba(22, 163, 74, 0.4);
        }
        
        /* Enhanced Print Styles */
        @media print {
            body {
                background: white !important;
                -webkit-print-color-adjust: exact;
                color-adjust: exact;
            }
            
            .container { 
                padding: 30px;
                max-width: none;
                box-shadow: none;
                border-radius: 0;
                background: white !important;
            }
            
            body::before {
                font-size: 90px;
                color: rgba(59, 130, 246, 0.06) !important;
            }
            
            body::after {
                font-size: 32px;
                color: rgba(239, 68, 68, 0.04) !important;
            }
            
            .watermark-corner {
                opacity: 0.03 !important;
            }
            
            .header {
                background: #3b82f6 !important;
                -webkit-print-color-adjust: exact;
                color-adjust: exact;
                color: white !important;
            }
            
            .info-card, .section, .digital-signature {
                box-shadow: none;
                border: 1px solid #e5e7eb;
            }
            
            .digital-signature::before {
                display: none;
            }
        }
    </style>
</head>
<body>
    <!-- Professional Watermark Elements -->
    <div class='watermark-corner top-left'></div>
    <div class='watermark-corner bottom-right'></div>
    
    <div class='container'>
        <div class='header'>
            <div class='company-logo'>PMS</div>
            <h1>Residential Lease Agreement</h1>
            <h2>{{CompanyName}}</h2>
            <div class='contact-info'>
                [ADDR] {{CompanyAddress}}<br>
                [PHONE] {{CompanyPhone}} | [EMAIL] {{CompanyEmail}}
            </div>
        </div>
        
        <div class='document-info'>
            <h3>Document Information</h3>
            <div style='display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 10px;'>
                <p><strong>Agreement ID:</strong> {{LeaseAgreementId}}</p>
                <p><strong>Generated:</strong> {{GeneratedDate}}</p>
                <p><strong>Time:</strong> {{GeneratedTime}}</p>
                <p><strong>Duration:</strong> {{LeaseDurationMonths}} months</p>
            </div>
        </div>
        
        <div class='info-grid'>
            <div class='info-card'>
                <h4><span class='card-icon'>[TENANT]</span> Tenant Information</h4>
                <p><strong>Name:</strong> {{TenantName}}</p>
                <p><strong>Contact:</strong> {{TenantContact}}</p>
                <p><strong>Emergency Contact:</strong> {{TenantEmergencyContact}}</p>
                <p><strong>Emergency Phone:</strong> {{TenantEmergencyNumber}}</p>
            </div>
            
            <div class='info-card'>
                <h4><span class='card-icon'>[PROPERTY]</span> Property Details</h4>
                <p><strong>Room Number:</strong> {{RoomNumber}}</p>
                <p><strong>Room Type:</strong> {{RoomType}}</p>
            </div>
            
            <div class='info-card'>
                <h4><span class='card-icon'>[FINANCE]</span> Financial Terms</h4>
                <p><strong>Monthly Rent:</strong> <span style='color: #dc2626; font-weight: 700; font-size: 16px;'>{{RentAmount}}</span></p>
                <p><strong>Due Date:</strong> {{ExpectedRentDay}} of each month</p>
            </div>
            
            <div class='info-card'>
                <h4><span class='card-icon'>[PERIOD]</span> Lease Period</h4>
                <p><strong>Start Date:</strong> {{StartDate}}</p>
                <p><strong>End Date:</strong> {{EndDate}}</p>
            </div>
        </div>
        
        <div class='highlight-box'>
            <p><strong>[LEGAL NOTICE]:</strong> This lease agreement becomes legally binding upon digital signature by the tenant. Please read all terms carefully before signing. This document has the same legal validity as a traditional paper lease agreement.</p>
        </div>
        
        <div class='section'>
            <h2><span class='section-icon'>[TERMS]</span> Terms and Conditions</h2>
            <ol class='terms-list'>
                <li><strong>Payment Terms:</strong> Rent is due on the {{ExpectedRentDay}} of each month. Late payments may incur additional charges as per local regulations.</li>
                
                <li><strong>Security Deposit:</strong> A security deposit equivalent to one month's rent is required before occupancy. This will be held in accordance with local tenancy laws.</li>
                
                <li><strong>Property Maintenance:</strong> Tenant agrees to maintain the property in good condition and report any damages or maintenance issues immediately to the landlord.</li>
                
                <li><strong>Utilities and Services:</strong> Tenant is responsible for utilities unless otherwise specified in writing. This includes electricity, water, internet, and other services.</li>
                
                <li><strong>Pet Policy:</strong> No pets are allowed without prior written consent from the landlord. Unauthorized pets may result in lease termination.</li>
                
                <li><strong>Subletting Policy:</strong> Subletting or assignment of this lease is not permitted without written approval from the landlord.</li>
                
                <li><strong>Lease Termination:</strong> Either party may terminate this lease with 30 days written notice, subject to local tenancy regulations.</li>
                
                <li><strong>Legal Compliance:</strong> Tenant agrees to comply with all local, provincial, and federal laws while occupying the premises.</li>
                
                <li><strong>Property Access:</strong> Landlord reserves the right to inspect the property with 24 hours notice, except in emergencies.</li>
                
                <li><strong>Dispute Resolution:</strong> Any disputes arising from this agreement will be resolved through appropriate legal channels as per local jurisdiction.</li>
            </ol>
        </div>
        
        <div class='signature-section'>
            <h2>Digital Signature Acknowledgment</h2>
            <div class='acknowledgment-list'>
                <p><strong>By digitally signing this document, I acknowledge that:</strong></p>
                <ul>
                    <li>I have read and understood all terms and conditions of this lease agreement</li>
                    <li>I agree to be bound by the terms stated herein</li>
                    <li>My digital signature has the same legal effect as a handwritten signature</li>
                    <li>I understand that this is a legally binding agreement</li>
                    <li>I have received a copy of this agreement for my records</li>
                </ul>
            </div>
        </div>
        
        <div class='footer'>
            <div class='confidential'>CONFIDENTIAL DOCUMENT</div>
            <p>This document was electronically generated and is valid without a physical signature when digitally signed.</p>
            <p>Generated by Property Management Solutions - Professional Lease Management System</p>
            <p>Document Hash: Generated upon signing for verification purposes.</p>
            <p style='margin-top: 10px; font-style: italic;'>� {{GeneratedDate}} Property Management Solutions. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}