/**
 * capture_screenshots.mjs
 * 
 * Script chụp màn hình minh chứng E2E cho Luận văn tốt nghiệp - Solaris
 * 
 * CÁCH CHẠY:
 *   node capture_screenshots.mjs
 * 
 * OUTPUT: Thư mục ./screenshots/ chứa ~12 ảnh PNG
 */

import { chromium } from 'playwright-core';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const CHROME_PATH = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
const BASE_URL = 'http://localhost:3000';
const API_URL = 'http://localhost:7070/api';
const SCREENSHOT_DIR = path.join(__dirname, 'screenshots');

const USER_CREDENTIALS = {
  username: '0772569363',
  password: 'Password123@'
};

if (!fs.existsSync(SCREENSHOT_DIR)) {
  fs.mkdirSync(SCREENSHOT_DIR, { recursive: true });
}

async function apiRequest(endpoint, options = {}) {
  const res = await fetch(${API_URL}, options);
  if (!res.ok) {
    const text = await res.text();
    throw new Error(API  []: );
  }
  return res.json();
}

async function snap(page, filename, description) {
  await page.waitForTimeout(600);
  const fullPath = path.join(SCREENSHOT_DIR, filename);
  await page.screenshot({ path: fullPath, fullPage: false });
  console.log(  📸 [Screenshot]  — );
  return fullPath;
}

async function loginUser() {
  console.log('\n🔑 Đăng nhập tài khoản test...');
  const data = await apiRequest('/shop/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(USER_CREDENTIALS)
  });
  console.log(✅ Đã đăng nhập:  ());
  return data;
}

async function clearCart(token) {
  try {
    await apiRequest('/shop/cart', {
      method: 'DELETE',
      headers: { Authorization: Bearer  }
    });
  } catch (_) {}
}

async function cleanupAiSessions(token) {
  try {
    const sessions = await apiRequest('/shop/ai/sessions', {
      headers: { Authorization: Bearer  }
    });
    if (Array.isArray(sessions)) {
      for (const s of sessions) {
        await apiRequest(/shop/ai/sessions/, {
          method: 'DELETE',
          headers: { Authorization: Bearer  }
        }).catch(() => {});
      }
    }
  } catch (_) {}
}

// FLOW A
async function captureFlowA(browser, authData) {
  console.log('\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
  console.log('📋 FLOW A — Mua hàng truyền thống (4 trang web)');
  console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');

  const context = await browser.newContext({ viewport: { width: 1366, height: 768 } });
  await context.addInitScript(({ token, customerInfo }) => {
    localStorage.setItem('solaris_shop_token', token);
    localStorage.setItem('solaris_shop_user', JSON.stringify(customerInfo));
  }, { token: authData.token, customerInfo: authData.customerInfo });

  const page = await context.newPage();
  await clearCart(authData.token);
  const t0 = performance.now();

  try {
    console.log('\n  [Bước 1/8] Trang chủ + Thanh tìm kiếm...');
    await page.goto(${BASE_URL}/, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(1000);
    const searchInput = page.locator('input[placeholder*="Tìm kiếm"]').first();
    await searchInput.waitFor({ state: 'visible', timeout: 10000 });
    await searchInput.fill('Mì Hảo Hảo');
    await snap(page, 'FlowA_Step1_Homepage_Search.png', 'Trang chủ — Nhập từ khóa "Mì Hảo Hảo"');
    await searchInput.press('Enter');

    console.log('  [Bước 2/8] Trang kết quả tìm kiếm...');
    const productCard = page.locator('a[href*="/san-pham/mi-hao-hao-tom-chua-cay"]').first();
    await productCard.waitFor({ state: 'visible', timeout: 20000 });
    await snap(page, 'FlowA_Step2_ProductList_SearchResults.png', 'Trang danh sách — Kết quả tìm kiếm');
    await productCard.click();

    console.log('  [Bước 3/8] Trang chi tiết sản phẩm...');
    const plusBtn = page.locator('button:has-text("+")').first();
    await plusBtn.waitFor({ state: 'visible', timeout: 20000 });
    await snap(page, 'FlowA_Step3_ProductDetail_Page.png', 'Trang chi tiết sản phẩm');
    await plusBtn.click();
    await page.waitForTimeout(400);

    console.log('  [Bước 4/8] Thêm vào giỏ hàng...');
    const addToCartBtn = page.locator('button:has-text("Thêm Vào Giỏ Hàng")').first();
    await addToCartBtn.waitFor({ state: 'visible', timeout: 10000 });
    await addToCartBtn.click();
    await page.waitForTimeout(800);
    await snap(page, 'FlowA_Step4_ProductDetail_AddedToCart.png', 'Đã thêm vào giỏ hàng (qty=2)');

    console.log('  [Bước 5/8] Trang Giỏ Hàng...');
    const cartLink = page.locator('a[href="/gio-hang"]').first();
    await cartLink.waitFor({ state: 'visible', timeout: 10000 });
    await cartLink.click();
    const proceedCheckoutBtn = page.locator('button:has-text("Tiến Hành Đặt Hàng")').first();
    await proceedCheckoutBtn.waitFor({ state: 'visible', timeout: 20000 });
    await snap(page, 'FlowA_Step5_CartPage.png', 'Trang giỏ hàng + nút Tiến Hành Đặt Hàng');

    console.log('  [Bước 6/8] Trang Thanh Toán...');
    await proceedCheckoutBtn.click();
    const codOption = page.locator('text=Khi nhận hàng (COD)').first();
    await codOption.waitFor({ state: 'visible', timeout: 20000 });
    await snap(page, 'FlowA_Step6_CheckoutPage_Form.png', 'Trang thanh toán — Form địa chỉ + phương thức');
    await codOption.click();
    await page.waitForTimeout(400);
    await snap(page, 'FlowA_Step7_CheckoutPage_COD_Selected.png', 'Đã chọn COD — sẵn sàng xác nhận');

    console.log('  [Bước 7/8] Xác nhận đặt hàng...');
    const confirmOrderBtn = page.locator('button:has-text("Xác Nhận & Đặt Hàng")').first();
    await confirmOrderBtn.waitFor({ state: 'visible', timeout: 10000 });
    await confirmOrderBtn.click();

    console.log('  [Bước 8/8] Trang xác nhận thành công...');
    await page.waitForURL(/.*\/tai-khoan\/don-hang\/(ORD-[\w-]+).*/, { timeout: 30000 });
    const orderCode = (page.url().match(/ORD-[\w-]+/) || ['ORD-UNKNOWN'])[0];
    await page.waitForTimeout(1000);
    await snap(page, 'FlowA_Step8_OrderSuccess_Page.png', Đơn hàng thành công — );

    const tTotal = ((performance.now() - t0) / 1000).toFixed(2);
    console.log(\n  ✅ FLOW A HOÀN TẤT —  | s);
  } catch (err) {
    console.error('\n  ❌ FLOW A LỖI:', err.message);
    await snap(page, 'FlowA_ERROR.png', 'Error state').catch(() => {});
  } finally {
    await context.close();
  }
}

// FLOW B
async function captureFlowB(browser, authData) {
  console.log('\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
  console.log('🤖 FLOW B — AI Chatbot 1-Click Checkout (1 màn hình)');
  console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');

  const sessionChatToken = screenshot_;
  const context = await browser.newContext({ viewport: { width: 1366, height: 768 } });
  await context.addInitScript(({ token, customerInfo, chatToken }) => {
    localStorage.setItem('solaris_shop_token', token);
    localStorage.setItem('solaris_shop_user', JSON.stringify(customerInfo));
    localStorage.setItem('solaris_chat_token', chatToken);
  }, { token: authData.token, customerInfo: authData.customerInfo, chatToken: sessionChatToken });

  const page = await context.newPage();
  await cleanupAiSessions(authData.token);
  const t0 = performance.now();

  try {
    console.log('\n  [Bước 1/3] Trang chủ — Mở AI chatbot widget...');
    await page.goto(${BASE_URL}/, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(1000);
    await snap(page, 'FlowB_Step1_Homepage_WithAIBubble.png', 'Trang chủ — Nút bubble AI chatbot');

    const aiToggleBtn = page.locator('button[aria-label="Trợ Lý Nông Sản AI"]').first();
    await aiToggleBtn.waitFor({ state: 'visible', timeout: 10000 });
    await aiToggleBtn.click();

    const chatInput = page.locator('input[data-testid="ai-chat-input"]').first();
    await chatInput.waitFor({ state: 'visible', timeout: 15000 });
    await page.waitForFunction(() => {
      const input = document.querySelector('input[data-testid="ai-chat-input"]');
      return input && !input.disabled;
    }, { timeout: 15000 });
    await snap(page, 'FlowB_Step2_ChatWidget_Opened.png', 'Widget AI đã mở — Sẵn sàng nhận yêu cầu');

    console.log('  [Bước 2/3] Gửi yêu cầu mua hàng bằng ngôn ngữ tự nhiên...');
    await chatInput.fill('Cho tôi mua 2 gói Mì Hảo Hảo Tôm Chua Cay');
    await snap(page, 'FlowB_Step3_ChatInput_OrderRequest.png', 'Nhập yêu cầu mua hàng bằng ngôn ngữ tự nhiên');
    await chatInput.press('Enter');

    console.log('  ⏳ Đang chờ AI xử lý và render InteractiveOrderCard...');
    await page.waitForSelector(
      'button:has-text("Xác Nhận & Thanh Toán"), button:has-text("Xác Nhận Đặt Đơn")',
      { timeout: 45000 }
    );
    await page.waitForTimeout(800);
    await snap(page, 'FlowB_Step4_InteractiveOrderCard_Rendered.png', 'AI render InteractiveOrderCard — Thông tin đặt hàng điền sẵn');

    console.log('  [Bước 3/3] Click xác nhận đặt đơn 1-click...');
    const confirmBtn = page.locator(
      'button:has-text("Xác Nhận & Thanh Toán"), button:has-text("Xác Nhận Đặt Đơn")'
    ).first();
    await confirmBtn.waitFor({ state: 'visible', timeout: 10000 });
    await confirmBtn.click();

    console.log('  ⏳ Đang chờ xác nhận đơn hàng thành công...');
    await page.waitForFunction(
      () => document.body.innerText.includes('ORD-'),
      { timeout: 30000 }
    ).catch(() => page.waitForTimeout(4000));

    await page.waitForTimeout(1000);
    await snap(page, 'FlowB_Step5_OrderSuccess_InChat.png', 'Đơn hàng thành công — Hiển thị trong chat (không rời trang)');

    const tTotal = ((performance.now() - t0) / 1000).toFixed(2);
    console.log(\n  ✅ FLOW B HOÀN TẤT — s);
  } catch (err) {
    console.error('\n  ❌ FLOW B LỖI:', err.message);
    await snap(page, 'FlowB_ERROR.png', 'Error state').catch(() => {});
  } finally {
    await context.close();
  }
}

async function main() {
  console.log('╔══════════════════════════════════════════════════════════════╗');
  console.log('║   Solaris E2E Screenshot Capture — Minh Chứng Luận Văn      ║');
  console.log('╚══════════════════════════════════════════════════════════════╝');
  console.log(📁 Thư mục lưu ảnh: );

  let authData;
  try {
    authData = await loginUser();
  } catch (err) {
    console.error('❌ Không thể đăng nhập. Hãy chắc chắn backend đang chạy tại port 7070.');
    process.exit(1);
  }

  const browser = await chromium.launch({
    executablePath: CHROME_PATH,
    headless: false,
    slowMo: 300,
    args: ['--start-maximized', '--disable-notifications', '--no-sandbox']
  });

  try {
    await captureFlowA(browser, authData);
    console.log('\n⏸  Nghỉ 2 giây trước khi chạy Flow B...\n');
    await new Promise(r => setTimeout(r, 2000));
    await captureFlowB(browser, authData);
  } finally {
    await browser.close();
  }

  const files = fs.readdirSync(SCREENSHOT_DIR).filter(f => f.endsWith('.png')).sort();
  console.log('\n╔══════════════════════════════════════════════════════════════╗');
  console.log('║                  ✅ CHỤP HÌNH HOÀN TẤT                       ║');
  console.log('╚══════════════════════════════════════════════════════════════╝');
  console.log(\n📁 );
  console.log(📸 Tổng:  ảnh\n);
  console.log('┌─ Flow A:');
  files.filter(f => f.startsWith('FlowA')).forEach(f => console.log(│  • ));
  console.log('└─ Flow B:');
  files.filter(f => f.startsWith('FlowB')).forEach(f => console.log(   • ));
}

main().catch(err => { console.error('💥', err); process.exit(1); });
