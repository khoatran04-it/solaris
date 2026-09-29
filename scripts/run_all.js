/**
 * SOLARIS RESEARCH EVALUATION SUITE — THESIS CHAPTER 4
 * Master Interactive Test Runner
 * 
 * Author: Solaris Engineering Team (Trần Đăng Khoa - IT22.504)
 */

const readline = require('readline');
const http = require('http');
const { runRQ1 } = require('./01_test_rq1_catalog_uom');
const { runRQ2 } = require('./02_test_rq2_concurrency_fefo');
const { runRQ3 } = require('./03_test_rq3_routing_coldchain');
const { runRQ4 } = require('./04_test_rq4_ai_chatbot');

const C = {
  reset: '\x1b[0m',
  bright: '\x1b[1m',
  dim: '\x1b[2m',
  red: '\x1b[31m',
  green: '\x1b[32m',
  yellow: '\x1b[33m',
  blue: '\x1b[34m',
  cyan: '\x1b[36m',
  white: '\x1b[37m',
  bgGreen: '\x1b[42m',
  bgBlue: '\x1b[44m'
};

function checkBackendHealth() {
  return new Promise((resolve) => {
    http.get('http://localhost:7070/api/evaluation/status', { timeout: 3000 }, (res) => {
      resolve(res.statusCode === 200);
    }).on('error', () => {
      resolve(false);
    });
  });
}

function showMenu() {
  console.clear();
  console.log(C.cyan + '='.repeat(90) + C.reset);
  console.log(C.bright + C.white + '     HỆ THỐNG THỰC NGHIỆM ĐỊNH LƯỢNG LUẬN VĂN TỐT NGHIỆP — SOLARIS PLATFORM' + C.reset);
  console.log(C.bright + C.yellow + '     Đề tài: Development of an Agricultural E-Commerce System Integrating ERP & AI Chatbot' + C.reset);
  console.log(C.dim + '     Tác giả: Trần Đăng Khoa (Mã đề tài: IT22.504)' + C.reset);
  console.log(C.cyan + '='.repeat(90) + C.reset);
  console.log('');
  console.log(C.bright + '  [1]' + C.reset + ' RQ1: Kiểm thử Mô hình Sản phẩm EAV & Đa quy cách bán (UoM Pricing)');
  console.log(C.bright + '  [2]' + C.reset + ' RQ2: Kiểm thử Thuật toán FEFO & Bùng nổ tải 500 CCU chống Bán âm kho');
  console.log(C.bright + '  [3]' + C.reset + ' RQ3: Kiểm thử Định tuyến Kho Haversine & Rào chắn Chuỗi lạnh 15km');
  console.log(C.bright + '  [4]' + C.reset + ' RQ4: Kiểm thử AI Chatbot (' + C.green + 'Quick Demo 10 câu' + C.reset + ' - Phục vụ Thuyết trình Hội đồng ~15s)');
  console.log(C.bright + '  [5]' + C.reset + ' RQ4: Kiểm thử AI Chatbot (' + C.yellow + 'Full Benchmark 50 câu' + C.reset + ' - Trọn vẹn số liệu Luận văn)');
  console.log(C.bright + '  [6]' + C.reset + ' ' + C.bgBlue + C.bright + ' CHẠY TOÀN BỘ TẤT CẢ THỰC NGHIỆM (RQ1 -> RQ4 Quick) ' + C.reset);
  console.log(C.bright + '  [0]' + C.reset + ' Thoát chương trình');
  console.log('');
  console.log(C.cyan + '='.repeat(90) + C.reset);
}

async function main() {
  const isHealthy = await checkBackendHealth();
  if (!isHealthy) {
    console.log(C.red + '\n[CẢNH BÁO]: Backend Solaris (http://localhost:7070) chưa được bật hoặc không phản hồi!' + C.reset);
    console.log(C.yellow + '-> Vui lòng mở 1 cửa sổ Terminal mới, di chuyển vào thư mục backend và chạy:' + C.reset);
    console.log(C.bright + '   cd d:\\Project\\Solaris\\backend ; dotnet run\n' + C.reset);
  }

  const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout
  });

  const promptChoice = () => {
    rl.question(C.bright + C.white + 'Nhập số kịch bản bạn muốn thực thi [0 - 6]: ' + C.reset, async (answer) => {
      const choice = answer.trim();

      if (choice === '0') {
        console.log('\nTạm biệt! Chúc bạn có buổi bảo vệ Luận văn thành công rực rỡ!\n');
        rl.close();
        process.exit(0);
      }

      console.log('\nĐang chuẩn bị thực thi...\n');

      try {
        if (choice === '1') {
          await runRQ1();
        } else if (choice === '2') {
          await runRQ2();
        } else if (choice === '3') {
          await runRQ3();
        } else if (choice === '4') {
          await runRQ4(true);
        } else if (choice === '5') {
          await runRQ4(false);
        } else if (choice === '6') {
          console.log(C.bgBlue + C.bright + ' >>> BẮT ĐẦU CHẠY TRỌN BỘ THỰC NGHIỆM CHƯƠNG 4 <<< ' + C.reset + '\n');
          await runRQ1();
          await runRQ2();
          await runRQ3();
          await runRQ4(true);
          console.log(C.bgGreen + C.bright + ' >>> HOÀN TẤT TOÀN BỘ 4 THỰC NGHIỆM CHƯƠNG 4 THÀNH CÔNG! <<< ' + C.reset + '\n');
        } else {
          console.log(C.red + 'Lựa chọn không hợp lệ. Vui lòng nhập từ 0 đến 6.' + C.reset);
        }
      } catch (err) {
        console.log(C.red + `Lỗi khi thực thi: ${err.message}` + C.reset);
      }

      console.log(C.dim + 'Nhấn Enter để quay lại Menu chính...' + C.reset);
      rl.once('line', () => {
        showMenu();
        promptChoice();
      });
    });
  };

  showMenu();
  promptChoice();
}

main().catch(console.error);
