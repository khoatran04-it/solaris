// File: load_test.js
// Giả lập liên tục gửi truy vấn đọc sản phẩm để xem có bị đơ không
const http = require('http');

const API_URL = 'http://localhost:5000/api/shop/products/xoai-cat-hoa-loc'; // Cổng backend của bạn
let total = 0;
let success = 0;
let failed = 0;

console.log('>>> BẮT ĐẦU BẮN TẢI: Giả lập khách đang liên tục xem sản phẩm...');

const interval = setInterval(() => {
    total++;
    const start = Date.now();
    
    http.get(API_URL, { timeout: 3000 }, (res) => {
        const time = Date.now() - start;
        if (res.statusCode === 200) {
            success++;
            process.stdout.write(`.` ); // Thành công thì in dấu chấm
        } else {
            failed++;
            console.log(`\n[LỖI HTTP ${res.statusCode}] Thời gian: ${time}ms`);
        }
    }).on('error', (err) => {
        failed++;
        console.log(`\n[LỖI TIMEOUT / TREO] Không phản hồi sau 3s!`);
    });

}, 50); // Cứ 50ms bắn 1 request (khoảng 20 req/s liên tục)

// Sau 15 giây thì dừng và in kết quả
setTimeout(() => {
    clearInterval(interval);
    console.log('\n\n' + '='.repeat(40));
    console.log(`Tổng request gửi đi: ${total}`);
    console.log(`Thành công (200 OK):  ${success} (${((success/total)*100).toFixed(1)}%)`);
    console.log(`Bị lỗi / Timeout:     ${failed} (${((failed/total)*100).toFixed(1)}%)`);
    console.log('='.repeat(40));
}, 15000);