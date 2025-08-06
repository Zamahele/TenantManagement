# Lease Status Mismatch Debug Guide

## Issue Summary
- **UI Shows**: "Awaiting Signature" 
- **Database Status**: Draft
- **QuickSign Query**: Looking for status = Sent (but lease is Draft)
- **Result**: No matching leases found

## Root Cause Analysis

The problem is a **status mapping inconsistency**:

### Expected Status Flow:
```
Draft ? Generated ? Sent ? Signed
  ?        ?         ?       ?
"Draft" ? "Generated" ? "Awaiting Signature" ? "Signed"
```

### What's Happening:
- Your lease has database status = `Draft`
- According to `GetStatusInfo()` function, `Draft` should display as "Draft"
- But you're seeing "Awaiting Signature" which corresponds to `Sent` status
- This suggests either:
  1. There's a caching issue
  2. The AutoMapper mapping is incorrect
  3. The lease status was recently changed but not refreshed

## Debugging Steps

### 1. **Run Diagnostic Action**
Navigate to: `/DigitalLease/DiagnoseStatusMismatch`

This will show you:
- ? Actual database status vs UI display mapping
- ? Whether HTML content exists
- ? Generation and send timestamps
- ? Summary of all lease statuses

### 2. **Test Enhanced QuickSign**
- Go to Tenant Profile ? Click "Digital Signing" card
- **Expected Result**: Enhanced error message explaining the mismatch

### 3. **Check Individual Lease Status**
Navigate to: `/DigitalLease/LeaseStatusInfo?leaseAgreementId=YOUR_LEASE_ID`

## Fixed Functionality

### ? Enhanced QuickSign Action
- **Before**: Failed silently when no Sent leases found
- **After**: Provides specific guidance for Draft status leases
- **New Logic**: 
  ```csharp
  // First look for Sent leases (proper workflow)
  // If none found, check Draft leases and explain the issue
  ```

### ? Status Diagnostic Tool
- **URL**: `/DigitalLease/DiagnoseStatusMismatch`
- **Shows**: Complete status mapping information
- **Detects**: UI vs Database inconsistencies

### ? Better Error Messages
- **Draft Status**: "Your lease is still in Draft status. The property manager needs to generate the digital lease document first."
- **Technical Info**: Shows lease ID and expected workflow
- **Actionable Guidance**: What needs to happen next

## Testing Your Issue

### Step 1: Verify Current Status
```
Navigate to: /DigitalLease/DiagnoseStatusMismatch
```
Expected output format:
```
Lease ID X: DB Status='Draft', UI Display='Draft', HasHtml=No, HasPdf=No, IsSigned=False
```

### Step 2: Test QuickSign
```
Go to Profile ? Click "Digital Signing" card
```
Expected behavior:
- If status is truly Draft: Clear error message explaining the issue
- If status is actually Sent: Should work normally

### Step 3: Manager Actions (if needed)
If the lease should be ready for signing:
```
POST /DigitalLease/RegenerateLeaseContent?leaseAgreementId=YOUR_ID
```

## Resolution Paths

### If Status Should Be Draft:
- ? Enhanced error messages will guide you
- ? Contact property manager to follow proper workflow
- ? Expected: Draft ? Generate ? Send ? Sign

### If Status Should Be Sent:
- ? Use diagnostic tool to verify actual status
- ? Manager can regenerate content if needed
- ? System will then work properly

### If UI Display Bug:
- ? Diagnostic tool will reveal the discrepancy
- ? May need to clear browser cache
- ? Check if page needs refresh

## Key URLs for Testing

- **Diagnostic**: `/DigitalLease/DiagnoseStatusMismatch`
- **Status Check**: `/DigitalLease/LeaseStatusInfo?leaseAgreementId=YOUR_ID`
- **Enhanced Quick Sign**: Profile ? Digital Signing card

The enhanced system now handles this edge case gracefully and provides clear guidance on what's wrong and how to fix it! ??

## Expected Resolution

After applying the fix:
1. **Clear Error Messages**: Instead of silent failure, you'll get specific guidance
2. **Status Transparency**: Diagnostic tool shows exactly what's in the database vs UI
3. **Actionable Steps**: Clear next steps based on actual lease status
4. **Manager Tools**: Repair functions for broken leases

Your QuickSign button will now work intelligently and guide you through resolving the status mismatch! ?