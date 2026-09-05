import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import ProductVariantForm from '../../../pages/productVariant/ProductVariantForm';
import { productVariantApi } from '../../../api/productVariantApi';
import { productApi } from '../../../api/productApi';
import { uomApi } from '../../../api/uomApi';
import { uomConversionApi } from '../../../api/uomConversionApi';

// Mock APIs
vi.mock('../../../api/productVariantApi', () => ({
  productVariantApi: {
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
  },
}));

vi.mock('../../../api/productApi', () => ({
  productApi: {
    getAllList: vi.fn(),
    getAttributesConfig: vi.fn(),
  },
}));

vi.mock('../../../api/uomApi', () => ({
  uomApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/uomConversionApi', () => ({
  uomConversionApi: {
    getAllList: vi.fn(),
  },
}));

/**
 * ============================================================================
 * MODULE 5: PRODUCT & PRICING
 * COMPONENT TEST: ProductVariantForm (Form Thêm / Sửa Biến Thể 3 Tabs & Bảng Giá)
 * ============================================================================
 */
describe('Module 05 - ProductVariantForm Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (productApi.getAllList as any).mockResolvedValue([{ id: 1, name: 'Táo Envy New Zealand' }]);
    (productApi.getAttributesConfig as any).mockResolvedValue([
      { id: 100, name: 'Độ ngọt Brix', isRequired: true },
      { id: 101, name: 'Vùng trồng', isRequired: false },
    ]);
    (uomApi.getAllList as any).mockResolvedValue([
      { id: 1, name: 'Kilogram', code: 'KG' },
      { id: 2, name: 'Thùng 10Kg', code: 'BOX' },
    ]);
    (uomConversionApi.getAllList as any).mockResolvedValue([
      {
        id: 1,
        fromUoMId: 2,
        fromUoMName: 'Thùng 10Kg',
        toUoMId: 1,
        toUoMName: 'Kilogram',
        conversionFactor: 10,
        productId: null,
        isActive: true,
      },
    ]);
  });

  // #region TC01: RENDER 3 TABS ĐIỀU HƯỚNG
  it('TC01 - Render đầy đủ 3 Tab điều hướng: Thông tin cơ bản, Thuộc tính và Quy cách bán hàng', async () => {
    render(
      <MemoryRouter initialEntries={['/product-variants/create']}>
        <Routes>
          <Route path="/product-variants/create" element={<ProductVariantForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText(/1. THÔNG TIN CƠ BẢN/i)).toBeInTheDocument();
      expect(screen.getByText(/2. THUỘC TÍNH CHI TIẾT/i)).toBeInTheDocument();
      expect(screen.getByText(/3. QUY CÁCH BÁN HÀNG/i)).toBeInTheDocument();
      expect(screen.getByPlaceholderText('VD: TH-500G')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('Thịt Heo Ba Chỉ - Khay 500g')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC02: VALIDATION TRƯỜNG BẮT BUỘC
  it('TC02 - Báo lỗi validation khi submit thiếu Sản phẩm gốc, mã SKU hoặc tên biến thể', async () => {
    render(
      <MemoryRouter initialEntries={['/product-variants/create']}>
        <Routes>
          <Route path="/product-variants/create" element={<ProductVariantForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText(/1. THÔNG TIN CƠ BẢN/i)).toBeInTheDocument();
    });

    // Chuyển sang Tab 3 để bấm Submit
    const pricingTab = screen.getByText(/3. QUY CÁCH BÁN HÀNG/i);
    fireEvent.click(pricingTab);

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      // Tự động chuyển về Tab 1 và báo lỗi
      expect(screen.getByText('Vui lòng chọn Sản phẩm gốc.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng nhập mã SKU.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng nhập tên biến thể.')).toBeInTheDocument();
    });

    expect(productVariantApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC03: CHUYỂN TAB VÀ TẢI THUỘC TÍNH ĐỘNG (EAV)
  it('TC03 - Tải thuộc tính động EAV khi người dùng chọn Sản phẩm gốc và chuyển sang Tab 2', async () => {
    render(
      <MemoryRouter initialEntries={['/product-variants/create']}>
        <Routes>
          <Route path="/product-variants/create" element={<ProductVariantForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText(/1. THÔNG TIN CƠ BẢN/i)).toBeInTheDocument();
      expect(screen.getByText('Chọn sản phẩm...')).toBeInTheDocument();
    });

    // Chọn Sản phẩm gốc ID = 1 qua FormSelect
    const productSelect = screen.getByText('Chọn sản phẩm...');
    fireEvent.click(productSelect);
    const option = await screen.findByText('Táo Envy New Zealand');
    fireEvent.click(option);

    // Chuyển sang Tab 2
    const attrTab = screen.getByText(/2. THUỘC TÍNH CHI TIẾT/i);
    fireEvent.click(attrTab);

    await waitFor(() => {
      expect(productApi.getAttributesConfig).toHaveBeenCalledWith(1);
      expect(screen.getByText('Độ ngọt Brix')).toBeInTheDocument();
      expect(screen.getByText('Vùng trồng')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC04: THAO TÁC BẢNG GIÁ QUY CÁCH (THÊM DÒNG, CHỌN MẶC ĐỊNH, XÓA DÒNG)
  it('TC04 - Thao tác thêm bảng giá quy cách bán hàng và đánh dấu giá mặc định', async () => {
    render(
      <MemoryRouter initialEntries={['/product-variants/create']}>
        <Routes>
          <Route path="/product-variants/create" element={<ProductVariantForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText(/3. QUY CÁCH BÁN HÀNG/i)).toBeInTheDocument();
    });

    // Chuyển sang Tab 3
    const pricingTab = screen.getByText(/3. QUY CÁCH BÁN HÀNG/i);
    fireEvent.click(pricingTab);

    // Bấm Thêm quy cách bán
    const addPriceBtn = screen.getByRole('button', { name: /THÊM QUY CÁCH BÁN/i });
    fireEvent.click(addPriceBtn);

    await waitFor(() => {
      expect(screen.getByTitle('Đang làm mặc định')).toBeInTheDocument();
    });

    // Thêm dòng thứ 2
    fireEvent.click(addPriceBtn);

    const defaultButtons = screen.getAllByTitle(/mặc định/i);
    expect(defaultButtons.length).toBe(2);

    // Bấm nút xóa dòng
    const deleteButtons = screen.getAllByTitle('Xóa quy cách');
    fireEvent.click(deleteButtons[1]);

    await waitFor(() => {
      const remainingDefaults = screen.getAllByTitle(/mặc định/i);
      expect(remainingDefaults.length).toBe(1);
    });
  });
  // #endregion

  // #region TC05: EDIT MODE - LOAD TOÀN BỘ DỮ LIỆU VÀ UPDATE THÀNH CÔNG
  it('TC05 - Load dữ liệu chi tiết ở chế độ Edit (kèm bảng giá và thuộc tính) rồi submit cập nhật', async () => {
    (productVariantApi.getById as any).mockResolvedValue({
      id: 1,
      code: 'SKU-TAO-1KG',
      name: 'Táo Envy Túi 1Kg',
      description: 'Táo ngon chuẩn New Zealand',
      imagePath: 'https://example.com/tao.jpg',
      inventoryGuideline: 100,
      isActive: true,
      productId: 1,
      attributes: [{ attributeDefinitionId: 100, attributeValue: '16' }],
      prices: [{ uoMId: 1, price: 120000, isDefault: true }],
    });
    (productVariantApi.update as any).mockResolvedValue({});

    render(
      <MemoryRouter initialEntries={['/product-variants/edit/1']}>
        <Routes>
          <Route path="/product-variants/edit/:id" element={<ProductVariantForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Chỉnh Sửa Biến Thể')).toBeInTheDocument();
      expect(screen.getByDisplayValue('SKU-TAO-1KG')).toBeInTheDocument();
      expect(screen.getByDisplayValue('Táo Envy Túi 1Kg')).toBeInTheDocument();
    });

    const nameInput = screen.getByDisplayValue('Táo Envy Túi 1Kg');
    fireEvent.change(nameInput, { target: { value: 'Táo Envy Túi 1Kg (Loại 1)' } });

    // Sang Tab 3 và Submit
    const pricingTab = screen.getByText(/3. QUY CÁCH BÁN HÀNG/i);
    fireEvent.click(pricingTab);

    const submitBtn = screen.getByRole('button', { name: /LƯU THAY ĐỔI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(productVariantApi.update).toHaveBeenCalledWith(
        1,
        expect.objectContaining({
          code: 'SKU-TAO-1KG',
          name: 'Táo Envy Túi 1Kg (Loại 1)',
        })
      );
    });
  });
  // #endregion

  // #region TC06: QUY CÁCH ĐÓNG GÓI, KÍCH THƯỚC VẬT LÝ VÀ TÍNH TOÁN CBM
  it('TC06 - Nhập kích thước Dài, Rộng, Cao tự động tính toán Thể tích CBM và lưu Khối lượng cả bì', async () => {
    render(
      <MemoryRouter initialEntries={['/product-variants/create']}>
        <Routes>
          <Route path="/product-variants/create" element={<ProductVariantForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByPlaceholderText('VD: TH-500G')).toBeInTheDocument();
    });

    // 1. Điền kích thước đóng gói ban đầu: 50 x 40 x 25 cm
    const lengthInput = screen.getByPlaceholderText('VD: 30');
    fireEvent.change(lengthInput, { target: { value: '50' } });

    const widthInput = screen.getByPlaceholderText('VD: 20');
    fireEvent.change(widthInput, { target: { value: '40' } });

    const heightInput = screen.getByPlaceholderText('VD: 15');
    fireEvent.change(heightInput, { target: { value: '25' } });

    // 2. Kiểm tra ô CBM tự động tính toán: 50 * 40 * 25 / 1,000,000 = 0.05
    const cbmInput = screen.getByPlaceholderText('Tự động tính') as HTMLInputElement;
    await waitFor(() => {
      expect(cbmInput.value).toBe('0.05');
    });

    // 3. Thay đổi kích thước sang kiện lớn: 100 x 50 x 40 cm -> CBM = 0.2
    fireEvent.change(lengthInput, { target: { value: '100' } });
    fireEvent.change(widthInput, { target: { value: '50' } });
    fireEvent.change(heightInput, { target: { value: '40' } });

    await waitFor(() => {
      expect(cbmInput.value).toBe('0.2');
    });

    // 4. Điền khối lượng cả bì (Gross Weight)
    const weightInput = screen.getByPlaceholderText('VD: 1.5') as HTMLInputElement;
    fireEvent.change(weightInput, { target: { value: '18.5' } });
    expect(weightInput.value).toBe('18.5');
  });
  // #endregion
});
