import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import UoMConversionForm from '../../../pages/uomConversion/UoMConversionForm';
import { uomConversionApi } from '../../../api/uomConversionApi';
import { uomApi } from '../../../api/uomApi';
import { productApi } from '../../../api/productApi';

// Mock API
vi.mock('../../../api/uomConversionApi', () => ({
  uomConversionApi: {
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
  },
}));

vi.mock('../../../api/uomApi', () => ({
  uomApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/productApi', () => ({
  productApi: {
    getAllList: vi.fn(),
  },
}));

/**
 * ============================================================================
 * 📦 MODULE 2: UNIT OF MEASURE (UoM)
 * 🧪 COMPONENT TEST: UoMConversionForm (Form Thiết lập Tỷ lệ Quy đổi)
 * ============================================================================
 */
describe('Module 02 - UoMConversionForm Component', () => {
  const mockUoMs = [
    { id: 1, code: 'TON', name: 'Tấn' },
    { id: 2, code: 'KG', name: 'Kilogram' },
  ];

  const mockProducts = [
    { id: 10, code: 'WATER', name: 'Nước khoáng Lavie' },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (uomApi.getAllList as any).mockResolvedValue(mockUoMs);
    (productApi.getAllList as any).mockResolvedValue(mockProducts);
  });

  // #region TC01: RENDER FORM THIẾT LẬP QUY ĐỔI VỚI 2 TABS
  /**
   * TC01: Form tạo mới phải hiển thị tiêu đề "Thêm Mới Tỷ Lệ Quy Đổi", 2 Tab chế độ.
   */
  it('TC01 - Render đầy đủ các trường và 2 Tabs loại hình quy đổi', async () => {
    render(
      <MemoryRouter initialEntries={['/uom-conversions/create']}>
        <Routes>
          <Route path="/uom-conversions/create" element={<UoMConversionForm />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Thêm Mới Tỷ Lệ Quy Đổi')).toBeInTheDocument();
    expect(screen.getByText('Tiêu chuẩn toàn cục')).toBeInTheDocument();
    expect(screen.getByText('Đặc thù sản phẩm')).toBeInTheDocument();
    expect(screen.getByText('Công Thức Toán Học')).toBeInTheDocument();
  });
  // #endregion

  // #region TC02: VALIDATION TRƯỜNG BẮT BUỘC
  /**
   * TC02: Báo lỗi khi chưa chọn ĐVT nguồn, ĐVT đích.
   */
  it('TC02 - Báo lỗi validation khi chưa chọn ĐVT nguồn và ĐVT đích', async () => {
    render(
      <MemoryRouter initialEntries={['/uom-conversions/create']}>
        <Routes>
          <Route path="/uom-conversions/create" element={<UoMConversionForm />} />
        </Routes>
      </MemoryRouter>
    );

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Chọn đơn vị nguồn.')).toBeInTheDocument();
      expect(screen.getByText('Chọn đơn vị đích (gốc).')).toBeInTheDocument();
    });

    expect(uomConversionApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC03: CHUYỂN TAB ĐẶC THÙ SẢN PHẨM
  /**
   * TC03: Click Tab "Đặc thù sản phẩm" hiển thị thêm ô chọn Sản phẩm áp dụng.
   */
  it('TC03 - Chuyển sang Tab "Đặc thù sản phẩm" hiển thị selector chọn Sản phẩm', async () => {
    render(
      <MemoryRouter initialEntries={['/uom-conversions/create']}>
        <Routes>
          <Route path="/uom-conversions/create" element={<UoMConversionForm />} />
        </Routes>
      </MemoryRouter>
    );

    const productTabBtn = screen.getByText('Đặc thù sản phẩm');
    fireEvent.click(productTabBtn);

    await waitFor(() => {
      expect(screen.getByText('Chọn sản phẩm áp dụng')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC04: CHẶN VÒNG LẶP TOÁN HỌC (FROM == TO)
  /**
   * TC04: Chọn ĐVT nguồn trùng với ĐVT đích sẽ báo lỗi vòng lặp.
   */
  it('TC04 - Báo lỗi khi chọn ĐVT đích trùng với ĐVT nguồn', async () => {
    render(
      <MemoryRouter initialEntries={['/uom-conversions/create']}>
        <Routes>
          <Route path="/uom-conversions/create" element={<UoMConversionForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(uomApi.getAllList).toHaveBeenCalled();
    });

    // Mở dropdown From UoM
    const fromTrigger = screen.getByText('VD: Thùng');
    fireEvent.click(fromTrigger);
    await waitFor(() => expect(screen.getByText('Tấn (TON)')).toBeInTheDocument());
    fireEvent.click(screen.getByText('Tấn (TON)'));

    // Mở dropdown To UoM
    const toTrigger = screen.getByText('VD: Lon');
    fireEvent.click(toTrigger);
    const toOptions = screen.getAllByText('Tấn (TON)');
    fireEvent.click(toOptions[toOptions.length - 1]);

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Đơn vị đích không được trùng đơn vị nguồn.')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC05: SUBMIT TẠO MỚI QUY TẮC THÀNH CÔNG
  /**
   * TC05: Chọn ĐVT nguồn = 1 (Tấn), ĐVT đích = 2 (Kg), hệ số = 1000 và submit thành công.
   */
  it('TC05 - Submit tạo mới quy tắc quy đổi thành công gọi API', async () => {
    (uomConversionApi.create as any).mockResolvedValue({ id: 1 });

    render(
      <MemoryRouter initialEntries={['/uom-conversions/create']}>
        <Routes>
          <Route path="/uom-conversions/create" element={<UoMConversionForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(uomApi.getAllList).toHaveBeenCalled();
    });

    // Chọn From UoM = 1 (Tấn)
    const fromTrigger = screen.getByText('VD: Thùng');
    fireEvent.click(fromTrigger);
    await waitFor(() => expect(screen.getByText('Tấn (TON)')).toBeInTheDocument());
    fireEvent.click(screen.getByText('Tấn (TON)'));

    // Nhập hệ số = 1000
    const factorInput = screen.getByPlaceholderText('VD: 24');
    fireEvent.change(factorInput, { target: { value: '1000' } });

    // Chọn To UoM = 2 (Kg)
    const toTrigger = screen.getByText('VD: Lon');
    fireEvent.click(toTrigger);
    await waitFor(() => expect(screen.getByText('Kilogram (KG)')).toBeInTheDocument());
    fireEvent.click(screen.getByText('Kilogram (KG)'));

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(uomConversionApi.create).toHaveBeenCalledWith({
        fromUoMId: 1,
        toUoMId: 2,
        conversionFactor: 1000,
        productId: null,
        isActive: true,
      });
    });
  });
  // #endregion
});
