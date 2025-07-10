window.printReceipt = function () {
  var receiptHtml = document.querySelector('#receiptModalBody .receipt-modal-scope').outerHTML;
  var printWindow = window.open('', '', 'width=800,height=600');
  printWindow.document.write(`
    <html>
      <head>
        <title>Payment Receipt</title>
        <link rel="stylesheet" href="/lib/bootstrap/dist/css/bootstrap.min.css" />
        <link rel="stylesheet" href="/css/receipt.css" />
        <style>
          body { background: #fff !important; }
        </style>
      </head>
      <body>
        ${receiptHtml}
        <script>
          window.onload = function() {
            window.focus();
            window.print();
            window.onafterprint = function() { window.close(); };
          };
        <\/script>
      </body>
    </html>
  `);
  printWindow.document.close();
};

window.shareReceipt = function () {
  var receiptContainer = document.querySelector('#receiptModalBody .receipt-modal-scope');
  if (!receiptContainer) {
    alert('Receipt not found.');
    return;
  }
  var rows = receiptContainer.querySelectorAll('.row.mb-2');
  var data = {};
  rows.forEach(function(row) {
    var label = row.querySelector('.col-5.fw-bold')?.innerText.trim();
    var value = row.querySelector('.col-7')?.innerText.trim();
    if (label && value) data[label] = value;
  });
  var text = `Payment receipt for ${data['Tenant'] || ''}, Room ${data['Room'] || ''}, Amount: ${data['Amount'] || ''}, Date: ${data['Date'] || ''}`;
  if (navigator.share) {
    navigator.share({
      title: 'Payment Receipt',
      text: text,
      url: window.location.href
    });
  } else {
    alert('Sharing is not supported in this browser.');
  }
};