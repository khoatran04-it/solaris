import { chromium } from 'playwright-core';
import fs from 'fs';
import path from 'path';

const CHROME_PATH = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
const BASE_URL = 'http://localhost:3000';
const API_URL = 'http://localhost:7070/api';

const USER_CREDENTIALS = {
  username: '0772569363',
  password: 'Password123@'
};

// Helper: Fetch API with error checking
async function apiRequest(endpoint, options = {}) {
  const url = `${API_URL}${endpoint}`;
  const res = await fetch(url, options);
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`API ${endpoint} failed [${res.status}]: ${text}`);
  }
  return res.json();
}

// 1. Authenticate user via backend API
async function loginUser() {
  console.log('Authenticating test account (0772569363)...');
  const data = await apiRequest('/shop/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(USER_CREDENTIALS)
  });
  console.log(`Logged in as: ${data.customerInfo.name} (${data.customerInfo.code})`);
  return data;
}

// 2. Clear customer cart before every test run
async function clearCart(token) {
  try {
    await apiRequest('/shop/cart', {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` }
    });
  } catch (err) {
    // Ignore if cart is already empty or error
  }
}

// Helper: Clear past AI sessions for clean test state
async function cleanupAiSessions(token) {
  try {
    const sessions = await apiRequest('/shop/ai/sessions', {
      headers: { Authorization: `Bearer ${token}` }
    });
    if (Array.isArray(sessions)) {
      for (const s of sessions) {
        await apiRequest(`/shop/ai/sessions/${s.id}`, {
          method: 'DELETE',
          headers: { Authorization: `Bearer ${token}` }
        }).catch(() => {});
      }
    }
  } catch (err) {}
}

// 3. Verify order exists in backend
async function verifyOrder(token, orderCode) {
  try {
    const order = await apiRequest(`/shop/orders/${orderCode}`, {
      method: 'GET',
      headers: { Authorization: `Bearer ${token}` }
    });
    return !!order && (order.orderCode === orderCode || order.id > 0);
  } catch (err) {
    console.error(`Verification failed for order ${orderCode}:`, err.message);
    return false;
  }
}

// 4. Flow A: Traditional Web Storefront Checkout Flow
async function runManualFlow(browser, authData, runIndex) {
  const context = await browser.newContext({
    viewport: { width: 1366, height: 768 }
  });

  // Inject authentication state into localStorage before page load
  await context.addInitScript(({ token, customerInfo }) => {
    localStorage.setItem('solaris_shop_token', token);
    localStorage.setItem('solaris_shop_user', JSON.stringify(customerInfo));
  }, { token: authData.token, customerInfo: authData.customerInfo });

  const page = await context.newPage();

  let requestCount = 0;
  let clickCount = 0;
  page.on('request', () => { requestCount++; });

  const trackClick = async (locator, description) => {
    clickCount++;
    await locator.click();
  };

  await clearCart(authData.token);

  console.log(`\n--- [FLOW A - Lượt ${runIndex}] Khởi động luồng Mua hàng Truyền thống ---`);
  const t0 = performance.now();

  try {
    // Step 1: Navigate to Home
    await page.goto(`${BASE_URL}/`, { waitUntil: 'domcontentloaded' });

    // Step 2: Search for "Mì Hảo Hảo"
    const searchInput = page.locator('input[placeholder*="Tìm kiếm"]').first();
    await searchInput.waitFor({ state: 'visible', timeout: 10000 });
    await searchInput.fill('Mì Hảo Hảo');
    clickCount++; // User interaction
    await searchInput.press('Enter');

    // Step 3: Wait for search results and click product "Mì Hảo Hảo Tôm Chua Cay"
    const productCard = page.locator('a[href*="/san-pham/mi-hao-hao-tom-chua-cay"]').first();
    await productCard.waitFor({ state: 'visible', timeout: 20000 });
    await trackClick(productCard, 'Chọn sản phẩm Mì Hảo Hảo');

    // Step 4: Product detail page -> Increase qty to 2 -> Add to cart
    const plusBtn = page.locator('button:has-text("+")').first();
    await plusBtn.waitFor({ state: 'visible', timeout: 20000 });
    await trackClick(plusBtn, 'Tăng số lượng lên 2');

    // Click "Thêm Vào Giỏ Hàng"
    const addToCartBtn = page.locator('button:has-text("Thêm Vào Giỏ Hàng")').first();
    await addToCartBtn.waitFor({ state: 'visible', timeout: 10000 });
    await trackClick(addToCartBtn, 'Thêm vào giỏ');

    // Wait for button state confirmation or small delay
    await page.waitForTimeout(600);

    // Step 5: Navigate to Cart
    const cartLink = page.locator('a[href="/gio-hang"]').first();
    await cartLink.waitFor({ state: 'visible', timeout: 10000 });
    await trackClick(cartLink, 'Mở Giỏ Hàng');

    // Step 6: Click "Tiến Hành Đặt Hàng"
    const proceedCheckoutBtn = page.locator('button:has-text("Tiến Hành Đặt Hàng")').first();
    await proceedCheckoutBtn.waitFor({ state: 'visible', timeout: 20000 });
    await trackClick(proceedCheckoutBtn, 'Tiến hành đặt hàng');

    // Step 7: Checkout page (/thanh-toan)
    // Select COD payment method
    const codOption = page.locator('text=Khi nhận hàng (COD)').first();
    await codOption.waitFor({ state: 'visible', timeout: 20000 });
    await trackClick(codOption, 'Chọn thanh toán COD');

    // Click "Xác Nhận & Đặt Hàng"
    const confirmOrderBtn = page.locator('button:has-text("Xác Nhận & Đặt Hàng")').first();
    await confirmOrderBtn.waitFor({ state: 'visible', timeout: 10000 });
    await trackClick(confirmOrderBtn, 'Xác nhận & Đặt hàng');

    // Step 8: Wait for order success URL (/tai-khoan/don-hang/ORD-...)
    await page.waitForURL(/.*\/tai-khoan\/don-hang\/(ORD-[\w-]+).*/, { timeout: 30000 });
    const currentUrl = page.url();
    const orderMatch = currentUrl.match(/ORD-[\w-]+/);
    const orderCode = orderMatch ? orderMatch[0] : 'ORD-UNKNOWN';

    const t1 = performance.now();
    const duration = (t1 - t0) / 1000;

    // Verify DB integrity
    const isVerified = await verifyOrder(authData.token, orderCode);

    console.log(`[FLOW A - Lượt ${runIndex}] HOÀN TẤT: Mã đơn=${orderCode}, Thời gian=${duration.toFixed(2)}s, Clicks=${clickCount}, Requests=${requestCount}, ACID=${isVerified ? 'Hợp lệ' : 'Thất bại'}`);

    await context.close();
    return {
      flow: 'A_Manual',
      runIndex,
      success: isVerified,
      durationSec: duration,
      clickCount,
      requestCount,
      orderCode
    };
  } catch (error) {
    const t1 = performance.now();
    console.error(`[FLOW A - Lượt ${runIndex}] LỖI:`, error.message);
    await context.close();
    return {
      flow: 'A_Manual',
      runIndex,
      success: false,
      durationSec: (t1 - t0) / 1000,
      clickCount,
      requestCount,
      error: error.message
    };
  }
}

// 5. Flow B: Conversational AI Commerce 1-Click Flow
async function runAiFlow(browser, authData, runIndex) {
  const context = await browser.newContext({
    viewport: { width: 1366, height: 768 }
  });

  const sessionChatToken = `bench_usr_${Date.now()}_${runIndex}`;

  // Inject authentication state and unique chat token
  await context.addInitScript(({ token, customerInfo, chatToken }) => {
    localStorage.setItem('solaris_shop_token', token);
    localStorage.setItem('solaris_shop_user', JSON.stringify(customerInfo));
    localStorage.setItem('solaris_chat_token', chatToken);
  }, { token: authData.token, customerInfo: authData.customerInfo, chatToken: sessionChatToken });

  const page = await context.newPage();

  let requestCount = 0;
  let clickCount = 0;
  page.on('request', req => {
    requestCount++;
    if (req.url().includes('/ai/')) {
      console.log(`   [AI Network Request]:`, req.method(), req.url(), req.postData()?.substring(0, 100));
    }
  });
  page.on('response', async res => {
    if (res.url().includes('/ai/')) {
      let body = '';
      try { body = await res.text(); } catch {}
      console.log(`   [AI Network Response]:`, res.status(), res.url(), body.substring(0, 200));
    }
  });
  page.on('console', msg => {
    console.log(`   [Browser Console ${msg.type()}]:`, msg.text());
  });

  const trackClick = async (locator, description) => {
    clickCount++;
    await locator.click();
  };

  await cleanupAiSessions(authData.token);

  console.log(`\n--- [FLOW B - Lượt ${runIndex}] Khởi động luồng Trợ Lý AI Chốt Đơn 1-Click ---`);
  const t0 = performance.now();

  try {
    // Step 1: Navigate to Home
    await page.goto(`${BASE_URL}/`, { waitUntil: 'domcontentloaded' });

    // Step 2: Click AI Chatbot Bubble
    const aiToggleBtn = page.locator('button[aria-label="Trợ Lý Nông Sản AI"]').first();
    await aiToggleBtn.waitFor({ state: 'visible', timeout: 10000 });
    await trackClick(aiToggleBtn, 'Mở AI Chatbot');

    // Step 3: Wait for session initialization and chat input to become enabled
    const chatInput = page.locator('input[data-testid="ai-chat-input"]').first();
    await chatInput.waitFor({ state: 'visible', timeout: 15000 });
    
    // Ensure input is not disabled (handleNewSession finished loading)
    await page.waitForFunction(() => {
      const input = document.querySelector('input[data-testid="ai-chat-input"]');
      return input && !input.disabled;
    }, { timeout: 15000 });

    await chatInput.fill('Bán cho tôi 2 gói mì hảo hảo');
    clickCount++; // Fill interaction

    // Submit via Enter on chatInput
    await chatInput.press('Enter');

    // Step 4: Wait for InteractiveOrderCard to render
    const confirmOrderCardBtn = page.locator('button:has-text("Xác Nhận Đặt Đơn"), button:has-text("Xác Nhận & Thanh Toán")').last();
    await confirmOrderCardBtn.waitFor({ state: 'visible', timeout: 45000 });

    // Step 5: Click 1-Click Confirm Button
    await trackClick(confirmOrderCardBtn, 'Chốt đơn 1-Click trên Thẻ tương tác');

    // Step 6: Wait for Order Success card & extract orderCode
    const successCard = page.locator('text=ĐÃ TẠO ĐƠN HÀNG THÀNH CÔNG!').last();
    await successCard.waitFor({ state: 'visible', timeout: 30000 });

    const orderCodeEl = page.locator('text=/Mã đơn:\\s*(ORD-[\\w-]+)/').last();
    const orderText = await orderCodeEl.innerText({ timeout: 5000 }).catch(() => '');
    const orderMatch = orderText.match(/ORD-[\w-]+/);
    const orderCode = orderMatch ? orderMatch[0] : 'ORD-UNKNOWN';

    const t1 = performance.now();
    const duration = (t1 - t0) / 1000;

    // Verify DB integrity
    const isVerified = await verifyOrder(authData.token, orderCode);

    console.log(`[FLOW B - Lượt ${runIndex}] HOÀN TẤT: Mã đơn=${orderCode}, Thời gian=${duration.toFixed(2)}s, Clicks=${clickCount}, Requests=${requestCount}, ACID=${isVerified ? 'Hợp lệ' : 'Thất bại'}`);

    await context.close();
    return {
      flow: 'B_AI',
      runIndex,
      success: isVerified,
      durationSec: duration,
      clickCount,
      requestCount,
      orderCode
    };
  } catch (error) {
    const t1 = performance.now();
    console.error(`[FLOW B - Lượt ${runIndex}] LỖI:`, error.message);
    try {
      await page.screenshot({ path: `error_flow_b_run_${runIndex}.png` });
      const widgetText = await page.evaluate(() => document.querySelector('.fixed.bottom-6.right-6')?.innerText);
      console.log(`   [Chat Widget Text on Error]:\n${widgetText}`);
    } catch {}
    await context.close();
    return {
      flow: 'B_AI',
      runIndex,
      success: false,
      durationSec: (t1 - t0) / 1000,
      clickCount,
      requestCount,
      error: error.message
    };
  }
}

// Helper: Calculate Statistics
function computeStats(samples) {
  if (!samples.length) return null;
  const n = samples.length;
  const sum = samples.reduce((acc, s) => acc + s.durationSec, 0);
  const mean = sum / n;
  const sorted = [...samples.map(s => s.durationSec)].sort((a, b) => a - b);
  const min = sorted[0];
  const max = sorted[sorted.length - 1];
  const p95 = sorted[Math.min(sorted.length - 1, Math.floor(sorted.length * 0.95))];
  
  const variance = samples.reduce((acc, s) => acc + Math.pow(s.durationSec - mean, 2), 0) / (n > 1 ? n - 1 : 1);
  const stdDev = Math.sqrt(variance);

  const meanClicks = samples.reduce((acc, s) => acc + s.clickCount, 0) / n;
  const meanRequests = samples.reduce((acc, s) => acc + s.requestCount, 0) / n;
  const successRate = (samples.filter(s => s.success).length / n) * 100;

  return {
    n,
    mean: parseFloat(mean.toFixed(2)),
    stdDev: parseFloat(stdDev.toFixed(2)),
    min: parseFloat(min.toFixed(2)),
    max: parseFloat(max.toFixed(2)),
    p95: parseFloat(p95.toFixed(2)),
    meanClicks: parseFloat(meanClicks.toFixed(1)),
    meanRequests: Math.round(meanRequests),
    successRate: parseFloat(successRate.toFixed(1))
  };
}

// Main benchmark runner
async function main() {
  const args = process.argv.slice(2);
  const isDryRun = args.includes('--dry-run');
  const isHeadless = args.includes('--headless');
  const runsArgIndex = args.indexOf('--runs');
  const totalRuns = isDryRun ? 1 : (runsArgIndex !== -1 ? parseInt(args[runsArgIndex + 1], 10) : 10);

  console.log('========================================================================');
  console.log('SOLARIS E2E CHECKOUT BENCHMARK');
  console.log(`Mode: ${isDryRun ? 'DRY-RUN (1 test run)' : `OFFICIAL BENCHMARK (${totalRuns} iterations)`}`);
  console.log(`Headless: ${isHeadless}`);
  console.log(`Browser: ${CHROME_PATH}`);
  console.log('========================================================================\n');

  const authData = await loginUser();

  console.log('Launching Google Chrome...');
  const browser = await chromium.launch({
    executablePath: CHROME_PATH,
    headless: isHeadless,
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  });

  const resultsA = [];
  const resultsB = [];

  for (let i = 1; i <= totalRuns; i++) {
    console.log(`\n>>> ITERATION ${i} / ${totalRuns} <<<`);
    
    // Run Flow A
    const resA = await runManualFlow(browser, authData, i);
    resultsA.push(resA);
    await new Promise(r => setTimeout(r, 1000));

    // Run Flow B
    const resB = await runAiFlow(browser, authData, i);
    resultsB.push(resB);
    await new Promise(r => setTimeout(r, 1000));
  }

  await browser.close();

  // Summary Statistics
  const validA = resultsA.filter(r => r.success);
  const validB = resultsB.filter(r => r.success);

  const statsA = computeStats(validA);
  const statsB = computeStats(validB);

  console.log('\n========================================================================');
  console.log('BẢNG TỔNG HỢP KẾT QUẢ THỰC NGHIỆM ĐỐI SÁNH (E2E BENCHMARK)');
  console.log('========================================================================');
  console.table({
    'Luồng A (Thủ công / Traditional)': {
      'Số lượt chạy': statsA ? statsA.n : 0,
      'Thời gian TB (Mean TCT)': statsA ? `${statsA.mean}s` : 'N/A',
      'Độ lệch chuẩn (StdDev σ)': statsA ? `±${statsA.stdDev}s` : 'N/A',
      'Nhanh nhất (Min)': statsA ? `${statsA.min}s` : 'N/A',
      'Chậm nhất (Max)': statsA ? `${statsA.max}s` : 'N/A',
      'Phân vị 95 (P95)': statsA ? `${statsA.p95}s` : 'N/A',
      'Số Clicks TB': statsA ? statsA.meanClicks : 'N/A',
      'Số Requests TB': statsA ? statsA.meanRequests : 'N/A',
      'ACID Success Rate': statsA ? `${statsA.successRate}%` : '0%'
    },
    'Luồng B (Trợ lý AI 1-Click)': {
      'Số lượt chạy': statsB ? statsB.n : 0,
      'Thời gian TB (Mean TCT)': statsB ? `${statsB.mean}s` : 'N/A',
      'Độ lệch chuẩn (StdDev σ)': statsB ? `±${statsB.stdDev}s` : 'N/A',
      'Nhanh nhất (Min)': statsB ? `${statsB.min}s` : 'N/A',
      'Chậm nhất (Max)': statsB ? `${statsB.max}s` : 'N/A',
      'Phân vị 95 (P95)': statsB ? `${statsB.p95}s` : 'N/A',
      'Số Clicks TB': statsB ? statsB.meanClicks : 'N/A',
      'Số Requests TB': statsB ? statsB.meanRequests : 'N/A',
      'ACID Success Rate': statsB ? `${statsB.successRate}%` : '0%'
    }
  });

  if (statsA && statsB) {
    const timeReduction = ((statsA.mean - statsB.mean) / statsA.mean * 100).toFixed(1);
    const clickReduction = ((statsA.meanClicks - statsB.meanClicks) / statsA.meanClicks * 100).toFixed(1);
    console.log(`KẾT LUẬN ĐỊNH LƯỢNG:`);
    console.log(`   - Thời gian hoàn tất đơn hàng giảm: ${timeReduction}% (${statsA.mean}s -> ${statsB.mean}s)`);
    console.log(`   - Số bước tương tác người dùng giảm: ${clickReduction}% (${statsA.meanClicks} clicks -> ${statsB.meanClicks} clicks)`);
  }

  // Save Raw Results to JSON
  const outputData = {
    executedAt: new Date().toISOString(),
    totalRuns,
    statsA,
    statsB,
    runsA: resultsA,
    runsB: resultsB
  };

  const outputPath = path.join(process.cwd(), 'results_e2e_benchmark.json');
  fs.writeFileSync(outputPath, JSON.stringify(outputData, null, 2), 'utf-8');
  console.log(`\nĐã lưu dữ liệu thực nghiệm chi tiết tại: ${outputPath}`);
}

main().catch(err => {
  console.error('Fatal benchmark error:', err);
  process.exit(1);
});
