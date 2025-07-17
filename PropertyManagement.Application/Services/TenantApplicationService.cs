using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Repositories;

namespace PropertyManagement.Application.Services;

public class TenantApplicationService : ITenantApplicationService
{
    private readonly IGenericRepository<Tenant> _tenantRepository;
    private readonly IGenericRepository<User> _userRepository;
    private readonly IGenericRepository<Room> _roomRepository;
    private readonly IMapper _mapper;

    public TenantApplicationService(
        IGenericRepository<Tenant> tenantRepository,
        IGenericRepository<User> userRepository,
        IGenericRepository<Room> roomRepository,
        IMapper mapper)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _roomRepository = roomRepository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<IEnumerable<TenantDto>>> GetAllTenantsAsync()
    {
        try
        {
            var tenants = await _tenantRepository.GetAllAsync(null, t => t.Room, t => t.User);
            var tenantDtos = _mapper.Map<IEnumerable<TenantDto>>(tenants);
            return ServiceResult<IEnumerable<TenantDto>>.Success(tenantDtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<TenantDto>>.Failure($"Error retrieving tenants: {ex.Message}");
        }
    }

    public async Task<ServiceResult<TenantDto>> GetTenantByIdAsync(int id)
    {
        try
        {
            var tenant = await _tenantRepository.GetByIdAsync(id);
            if (tenant == null)
            {
                return ServiceResult<TenantDto>.Failure("Tenant not found");
            }

            var tenantDto = _mapper.Map<TenantDto>(tenant);
            return ServiceResult<TenantDto>.Success(tenantDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<TenantDto>.Failure($"Error retrieving tenant: {ex.Message}");
        }
    }

    public async Task<ServiceResult<TenantDto>> GetTenantByUserIdAsync(int userId)
    {
        try
        {
            var tenants = await _tenantRepository.GetAllAsync(t => t.UserId == userId, t => t.Room, t => t.User);
            var tenant = tenants.FirstOrDefault();
            
            if (tenant == null)
            {
                return ServiceResult<TenantDto>.Failure("Tenant not found");
            }

            var tenantDto = _mapper.Map<TenantDto>(tenant);
            return ServiceResult<TenantDto>.Success(tenantDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<TenantDto>.Failure($"Error retrieving tenant: {ex.Message}");
        }
    }

    public async Task<ServiceResult<TenantDto>> CreateTenantAsync(CreateTenantDto createTenantDto)
    {
        try
        {
            // Business rule: Validate password strength
            if (string.IsNullOrWhiteSpace(createTenantDto.Password) || createTenantDto.Password.Length < 8)
            {
                return ServiceResult<TenantDto>.Failure("Password must be at least 8 characters long");
            }

            // Business rule: Check for duplicate username
            var usernameValidation = await ValidateUsernameAsync(createTenantDto.Username);
            if (!usernameValidation.IsSuccess)
            {
                return ServiceResult<TenantDto>.Failure(usernameValidation.ErrorMessage);
            }

            // Business rule: Check for duplicate contact
            var contactValidation = await ValidateContactAsync(createTenantDto.Contact);
            if (!contactValidation.IsSuccess)
            {
                return ServiceResult<TenantDto>.Failure(contactValidation.ErrorMessage);
            }

            // Business rule: Validate room exists
            var room = await _roomRepository.GetByIdAsync(createTenantDto.RoomId);
            if (room == null)
            {
                return ServiceResult<TenantDto>.Failure("Selected room does not exist");
            }

            // Create user account
            var user = new User
            {
                Username = createTenantDto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(createTenantDto.Password),
                Role = "Tenant"
            };

            await _userRepository.AddAsync(user);

            // Create tenant
            var tenant = new Tenant
            {
                FullName = createTenantDto.FullName,
                Contact = createTenantDto.Contact,
                EmergencyContactName = createTenantDto.EmergencyContactName,
                EmergencyContactNumber = createTenantDto.EmergencyContactNumber,
                RoomId = createTenantDto.RoomId,
                UserId = user.UserId
            };

            await _tenantRepository.AddAsync(tenant);

            var tenantDto = _mapper.Map<TenantDto>(tenant);
            return ServiceResult<TenantDto>.Success(tenantDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<TenantDto>.Failure($"Error creating tenant: {ex.Message}");
        }
    }

    public async Task<ServiceResult<TenantDto>> UpdateTenantAsync(int id, UpdateTenantDto updateTenantDto)
    {
        try
        {
            var tenant = await _tenantRepository.GetByIdAsync(id);
            if (tenant == null)
            {
                return ServiceResult<TenantDto>.Failure("Tenant not found");
            }

            // Business rule: Validate password strength if provided
            if (!string.IsNullOrWhiteSpace(updateTenantDto.Password) && updateTenantDto.Password.Length < 8)
            {
                return ServiceResult<TenantDto>.Failure("Password must be at least 8 characters long");
            }

            // Business rule: Check for duplicate contact (excluding current tenant)
            var contactValidation = await ValidateContactAsync(updateTenantDto.Contact, id);
            if (!contactValidation.IsSuccess)
            {
                return ServiceResult<TenantDto>.Failure(contactValidation.ErrorMessage);
            }

            // Business rule: Validate room exists
            var room = await _roomRepository.GetByIdAsync(updateTenantDto.RoomId);
            if (room == null)
            {
                return ServiceResult<TenantDto>.Failure("Selected room does not exist");
            }

            // Update tenant properties
            tenant.FullName = updateTenantDto.FullName;
            tenant.Contact = updateTenantDto.Contact;
            tenant.EmergencyContactName = updateTenantDto.EmergencyContactName;
            tenant.EmergencyContactNumber = updateTenantDto.EmergencyContactNumber;
            tenant.RoomId = updateTenantDto.RoomId;

            await _tenantRepository.UpdateAsync(tenant);

            // Update user account if username or password changed
            if (!string.IsNullOrWhiteSpace(updateTenantDto.Username) || !string.IsNullOrWhiteSpace(updateTenantDto.Password))
            {
                var user = await _userRepository.GetByIdAsync(tenant.UserId);
                if (user != null)
                {
                    if (!string.IsNullOrWhiteSpace(updateTenantDto.Username) && user.Username != updateTenantDto.Username)
                    {
                        // Check for duplicate username
                        var usernameValidation = await ValidateUsernameAsync(updateTenantDto.Username, user.UserId);
                        if (!usernameValidation.IsSuccess)
                        {
                            return ServiceResult<TenantDto>.Failure(usernameValidation.ErrorMessage);
                        }
                        user.Username = updateTenantDto.Username;
                    }

                    if (!string.IsNullOrWhiteSpace(updateTenantDto.Password))
                    {
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateTenantDto.Password);
                    }

                    await _userRepository.UpdateAsync(user);
                }
            }

            var tenantDto = _mapper.Map<TenantDto>(tenant);
            return ServiceResult<TenantDto>.Success(tenantDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<TenantDto>.Failure($"Error updating tenant: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteTenantAsync(int id)
    {
        try
        {
            var tenant = await _tenantRepository.GetByIdAsync(id);
            if (tenant == null)
            {
                return ServiceResult<bool>.Failure("Tenant not found");
            }

            // Delete associated user account
            var user = await _userRepository.GetByIdAsync(tenant.UserId);
            if (user != null)
            {
                await _userRepository.DeleteAsync(user);
            }

            await _tenantRepository.DeleteAsync(tenant);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Failure($"Error deleting tenant: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> ValidateUsernameAsync(string username, int? excludeUserId = null)
    {
        try
        {
            var existingUsers = await _userRepository.GetAllAsync(u => u.Username == username);
            if (excludeUserId.HasValue)
            {
                existingUsers = existingUsers.Where(u => u.UserId != excludeUserId.Value);
            }

            if (existingUsers.Any())
            {
                return ServiceResult<bool>.Failure("Username already exists");
            }

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Failure($"Error validating username: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> ValidateContactAsync(string contact, int? excludeTenantId = null)
    {
        try
        {
            var existingTenants = await _tenantRepository.GetAllAsync(t => t.Contact == contact);
            if (excludeTenantId.HasValue)
            {
                existingTenants = existingTenants.Where(t => t.TenantId != excludeTenantId.Value);
            }

            if (existingTenants.Any())
            {
                return ServiceResult<bool>.Failure("Contact number already exists");
            }

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Failure($"Error validating contact: {ex.Message}");
        }
    }

    public async Task<ServiceResult<UserDto>> AuthenticateAsync(string username, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return ServiceResult<UserDto>.Failure("Username and password are required");
            }

            var users = await _userRepository.GetAllAsync(u => u.Username == username);
            var user = users.FirstOrDefault();

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                // Add delay to prevent brute force attacks
                await Task.Delay(1000);
                return ServiceResult<UserDto>.Failure("Invalid username or password");
            }

            var userDto = _mapper.Map<UserDto>(user);
            return ServiceResult<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<UserDto>.Failure($"Authentication failed: {ex.Message}");
        }
    }

    public async Task<ServiceResult<TenantDto>> RegisterTenantAsync(RegisterTenantDto registerTenantDto)
    {
        try
        {
            // Business rule: Validate password strength
            if (string.IsNullOrWhiteSpace(registerTenantDto.Password) || registerTenantDto.Password.Length < 8)
            {
                return ServiceResult<TenantDto>.Failure("Password must be at least 8 characters long");
            }

            // Business rule: Check for duplicate username
            var usernameValidation = await ValidateUsernameAsync(registerTenantDto.Username);
            if (!usernameValidation.IsSuccess)
            {
                return ServiceResult<TenantDto>.Failure(usernameValidation.ErrorMessage);
            }

            // Business rule: Check for duplicate contact
            var contactValidation = await ValidateContactAsync(registerTenantDto.Contact);
            if (!contactValidation.IsSuccess)
            {
                return ServiceResult<TenantDto>.Failure(contactValidation.ErrorMessage);
            }

            // Create user account
            var user = new User
            {
                Username = registerTenantDto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerTenantDto.Password),
                Role = "Tenant"
            };

            await _userRepository.AddAsync(user);

            // Create tenant
            var tenant = new Tenant
            {
                FullName = registerTenantDto.FullName,
                Contact = registerTenantDto.Contact,
                EmergencyContactName = registerTenantDto.EmergencyContactName,
                EmergencyContactNumber = registerTenantDto.EmergencyContactNumber,
                RoomId = registerTenantDto.RoomId,
                UserId = user.UserId
            };

            await _tenantRepository.AddAsync(tenant);

            var tenantDto = _mapper.Map<TenantDto>(tenant);
            return ServiceResult<TenantDto>.Success(tenantDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<TenantDto>.Failure($"Error registering tenant: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> ChangePasswordAsync(int tenantId, string currentPassword, string newPassword)
    {
        try
        {
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);
            if (tenant == null)
            {
                return ServiceResult<bool>.Failure("Tenant not found");
            }

            var user = await _userRepository.GetByIdAsync(tenant.UserId);
            if (user == null)
            {
                return ServiceResult<bool>.Failure("User account not found");
            }

            // Business rule: Verify current password
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                return ServiceResult<bool>.Failure("Current password is incorrect");
            }

            // Business rule: Validate new password strength
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                return ServiceResult<bool>.Failure("New password must be at least 8 characters long");
            }

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _userRepository.UpdateAsync(user);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Failure($"Error changing password: {ex.Message}");
        }
    }

    public async Task<ServiceResult<TenantDto>> UpdateProfileAsync(int tenantId, UpdateProfileDto updateProfileDto)
    {
        try
        {
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);
            if (tenant == null)
            {
                return ServiceResult<TenantDto>.Failure("Tenant not found");
            }

            // Business rule: Check for duplicate contact (excluding current tenant)
            var contactValidation = await ValidateContactAsync(updateProfileDto.Contact, tenantId);
            if (!contactValidation.IsSuccess)
            {
                return ServiceResult<TenantDto>.Failure(contactValidation.ErrorMessage);
            }

            // Update only profile fields
            tenant.FullName = updateProfileDto.FullName;
            tenant.Contact = updateProfileDto.Contact;
            tenant.EmergencyContactName = updateProfileDto.EmergencyContactName;
            tenant.EmergencyContactNumber = updateProfileDto.EmergencyContactNumber;

            await _tenantRepository.UpdateAsync(tenant);

            var tenantDto = _mapper.Map<TenantDto>(tenant);
            return ServiceResult<TenantDto>.Success(tenantDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<TenantDto>.Failure($"Error updating profile: {ex.Message}");
        }
    }
}