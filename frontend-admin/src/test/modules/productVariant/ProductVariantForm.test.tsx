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
    create: vi.fn(),
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
      expect(screen.getByPlaceholderText('VD: SKU-DAUTAY-500G')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('VD: Dâu Tây Đà Lạt - Hộp 500g')).toBeInTheDocument();
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
      expect(screen.getByPlaceholderText('VD: SKU-DAUTAY-500G')).toBeInTheDocument();
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

  // #region TC07: TỰ ĐỘNG NHẬN DIỆN VÀ ĐIỀN ĐƠN VỊ TÍNH CƠ SỞ VÀO DÒNG 1 KHI CHUYỂN TAB QUY CÁCH
  it('TC07 - Tự động nhận diện Đơn vị tính cơ sở của sản phẩm và điền vào dòng đầu tiên khi chuyển sang Tab 3', async () => {
    (productApi.getAllList as any).mockResolvedValue([
      { id: 1, name: 'Táo Envy New Zealand', baseUoMId: 1, baseUoMName: 'Kilogram' },
    ]);

    render(
      <MemoryRouter initialEntries={['/product-variants/create']}>
        <Routes>
          <Route path="/product-variants/create" element={<ProductVariantForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Chọn sản phẩm...')).toBeInTheDocument();
    });

    // 1. Chọn Sản phẩm gốc có baseUoMId = 1 (Kilogram)
    const productSelect = screen.getByText('Chọn sản phẩm...');
    fireEvent.click(productSelect);
    const option = await screen.findByText('Táo Envy New Zealand');
    fireEvent.click(option);

    // 2. Chuyển sang Tab 3 (Quy cách bán hàng)
    const pricingTab = screen.getByText(/3. QUY CÁCH BÁN HÀNG/i);
    fireEvent.click(pricingTab);

    // 3. Kiểm tra dòng đầu tiên tự động nhận diện và điền sẵn Kilogram (Đơn vị cơ sở)
    await waitFor(() => {
      expect(screen.getByText(/Đơn vị cơ sở \(Kilogram\)/i)).toBeInTheDocument();
      expect(screen.getByTitle('Đang làm mặc định')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC08: QUY ĐỔI ĐẶC THÙ KHÓA ĐƠN VỊ CƠ SỞ VÀ TỰ ĐỘNG ĐIỀN ĐƠN VỊ LỚN VÀO BẢNG GIÁ
  it('TC08 - Cấu hình quy đổi nhanh: Đơn vị đích bị khóa theo ĐV cơ sở của sản phẩm và tự động điền ĐVT LỚN vào bảng quy cách', async () => {
    (productApi.getAllList as any).mockResolvedValue([
      { id: 1, name: 'Táo Envy New Zealand', baseUoMId: 1, baseUoMName: 'Kilogram' },
    ]);
    (uomConversionApi.create as any).mockResolvedValue({
      id: 99,
      fromUoMId: 2,
      fromUoMName: 'Thùng 10Kg',
      toUoMId: 1,
      toUoMName: 'Kilogram',
      conversionFactor: 10,
      productId: 1,
      isActive: true,
    });

    render(
      <MemoryRouter initialEntries={['/product-variants/create']}>
        <Routes>
          <Route path="/product-variants/create" element={<ProductVariantForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Chọn sản phẩm...')).toBeInTheDocument();
    });

    // 1. Chọn Sản phẩm gốc
    fireEvent.click(screen.getByText('Chọn sản phẩm...'));
    fireEvent.click(await screen.findByText('Táo Envy New Zealand'));

    // 2. Chuyển sang Tab 3
    fireEvent.click(screen.getByText(/3. QUY CÁCH BẢNG HÀNG|3. QUY CÁCH BÁN HÀNG/i));

    // 3. Bấm nút "Cấu hình quy đổi đặc thù"
    const openModalBtn = await screen.findByText(/Cấu hình quy đổi đặc thù/i);
    fireEvent.click(openModalBtn);

    // 4. Modal hiển thị: Kiểm tra Đơn vị cơ sở đã được khóa cố định theo sản phẩm
    await waitFor(() => {
      expect(screen.getByText(/Cấu Hình Quy Đổi Nhanh/i)).toBeInTheDocument();
      expect(screen.getByText(/Cố định theo đơn vị cơ sở/i)).toBeInTheDocument();
    });

    // 5. Chọn ĐVT lớn (Thùng 10Kg)
    const fromSelect = screen.getByText('Chọn ĐVT đóng gói...');
    fireEvent.click(fromSelect);
    const boxOption = await screen.findByText('Thùng 10Kg');
    fireEvent.click(boxOption);

    // 6. Nhập hệ số quy đổi
    const factorInput = screen.getByPlaceholderText('VD: 24');
    fireEvent.change(factorInput, { target: { value: '10' } });

    // 7. Bấm "Lưu Quy Đổi"
    const submitBtn = screen.getByText('Lưu Quy Đổi');
    fireEvent.click(submitBtn);

    // 8. Kiểm tra sau khi lưu: Bảng quy cách bán hàng tự động xuất hiện ĐVT lớn "Thùng 10Kg"
    await waitFor(() => {
      expect(screen.getByText(/Đã lưu quy đổi/i)).toBeInTheDocument();
      expect(screen.getAllByText(/Thùng 10Kg/i).length).toBeGreaterThan(0);
    });
  });
  // #endregion

  // #region TC09: BẤM CẤU HÌNH NGAY TẠI DÒNG QUY CÁCH CỤ THỂ
  it('TC09 - Bấm Cấu hình ngay tại dòng quy cách: Sau khi lưu, tự động điền ĐVT LỚN vào chính dòng đó', async () => {
    (productApi.getAllList as any).mockResolvedValue([
      { id: 1, name: 'Táo Envy New Zealand', baseUoMId: 1, baseUoMName: 'Kilogram' },
    ]);
    (uomApi.getAllList as any).mockResolvedValue([
      { id: 1, name: 'Kilogram', code: 'KG' },
      { id: 2, name: 'Thùng 10Kg', code: 'BOX' },
      { id: 3, name: 'Hộp 500g', code: 'BOX500' },
    ]);
    (uomConversionApi.getAllList as any).mockResolvedValue([]); // Chưa có quy đổi nào
    (uomConversionApi.create as any).mockResolvedValue({
      id: 101,
      fromUoMId: 3,
      fromUoMName: 'Hộp 500g',
      toUoMId: 1,
      toUoMName: 'Kilogram',
      conversionFactor: 0.5,
      productId: 1,
      isActive: true,
    });

    render(
      <MemoryRouter initialEntries={['/product-variants/create']}>
        <Routes>
          <Route path="/product-variants/create" element={<ProductVariantForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Chọn sản phẩm...')).toBeInTheDocument();
    });

    // 1. Chọn Sản phẩm gốc
    fireEvent.click(screen.getByText('Chọn sản phẩm...'));
    fireEvent.click(await screen.findByText('Táo Envy New Zealand'));

    // 2. Chuyển sang Tab 3
    fireEvent.click(screen.getByText(/3. QUY CÁCH BẢNG HÀNG|3. QUY CÁCH BÁN HÀNG/i));

    // 3. Thêm 1 dòng quy cách bán mới
    const addRowBtn = screen.getByText(/THÊM QUY CÁCH BÁN/i);
    fireEvent.click(addRowBtn);

    // 4. Ở dòng thứ 2: Chọn ĐVT chưa có quy đổi (Hộp 500g)
    const selectInputs = screen.getAllByText('Chọn ĐVT...');
    fireEvent.click(selectInputs[0]); // Mở dropdown dòng 2
    fireEvent.click(await screen.findByText('Hộp 500g'));

    // 5. Kiểm tra cảnh báo "Chưa cấu hình quy đổi" và bấm "Cấu hình ngay"
    const configNowBtn = await screen.findByText(/Cấu hình ngay/i);
    fireEvent.click(configNowBtn);

    // 6. Modal mở ra, nhập hệ số và Lưu
    await waitFor(() => {
      expect(screen.getByText(/Cấu Hình Quy Đổi Nhanh/i)).toBeInTheDocument();
    });

    const factorInput = screen.getByPlaceholderText('VD: 24');
    fireEvent.change(factorInput, { target: { value: '0.5' } });

    const submitBtn = screen.getByText('Lưu Quy Đổi');
    fireEvent.click(submitBtn);

    // 7. Kiểm tra sau khi lưu: Dòng quy cách thứ 2 đã được gán và hiển thị thành công
    await waitFor(() => {
      expect(screen.getByText(/Đã lưu quy đổi/i)).toBeInTheDocument();
      expect(screen.getAllByText(/Hộp 500g/i).length).toBeGreaterThan(0);
    });
  });
  // #endregion
});
