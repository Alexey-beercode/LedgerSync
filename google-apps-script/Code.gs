/**
 * LedgerSync — Google Apps Script Web App that writes sync runs into a Google Sheet.
 *
 * Deploy: Extensions → Apps Script → paste this file → Deploy → New deployment →
 * type "Web app", execute as "Me", access "Anyone with the link".
 * Put the deployment URL into GoogleSheets:AppsScriptUrl (user-secrets / env), never into git.
 *
 * Rows are upserted by runId: retries and manual resends never create duplicates.
 */

const SHEET_NAME = 'SyncLog';
const HEADERS = [
  'RunId', 'StartedAt', 'FinishedAt', 'Status', 'DocumentsReceived',
  'DocumentsCreated', 'DocumentsUpdated', 'DocumentsSkipped', 'ErrorMessage'
];

function doPost(e) {
  // Serializes concurrent requests so two writes cannot both append the same runId.
  const lock = LockService.getScriptLock();
  try {
    lock.waitLock(10000);

    const payload = JSON.parse(e.postData.contents);
    if (!payload.runId) {
      return json({ ok: false, error: 'runId is required' });
    }

    const sheet = getSheet();
    const row = [
      payload.runId,
      payload.startedAt || '',
      payload.finishedAt || '',
      payload.status || '',
      payload.documentsReceived || 0,
      payload.documentsCreated || 0,
      payload.documentsUpdated || 0,
      payload.documentsSkipped || 0,
      payload.errorMessage || ''
    ];

    const existing = sheet.getRange('A:A').createTextFinder(payload.runId).matchEntireCell(true).findNext();
    if (existing) {
      sheet.getRange(existing.getRow(), 1, 1, row.length).setValues([row]);
    } else {
      sheet.appendRow(row);
    }

    return json({ ok: true });
  } catch (err) {
    return json({ ok: false, error: String(err) });
  } finally {
    lock.releaseLock();
  }
}

function getSheet() {
  const spreadsheet = SpreadsheetApp.getActiveSpreadsheet();
  let sheet = spreadsheet.getSheetByName(SHEET_NAME);
  if (!sheet) {
    sheet = spreadsheet.insertSheet(SHEET_NAME);
  }
  if (sheet.getLastRow() === 0) {
    sheet.appendRow(HEADERS);
  }
  return sheet;
}

function json(body) {
  return ContentService.createTextOutput(JSON.stringify(body)).setMimeType(ContentService.MimeType.JSON);
}
