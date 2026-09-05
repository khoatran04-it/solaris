import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import UoMCategoryForm from '../../../pages/uomCategory/UoMCategoryForm';
import { uomCategoryApi } from '../../../api/uomCategoryApi';
import { uomApi } from '../../../api/uomApi';

// Mock API
vi.mock('../../../api/uomCategoryApi', () => ({
  uomCategoryApi: {
    getAllList: vi.fn(),
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

/**
 * ============================================================================
 * MODULE 2: UNIT OF MEASURE (UoM)
 * COMPONENT TEST: UoMCategoryForm (Form Thêm mới & Chỉnh sửa Nhóm ĐVT)
 * ============================================================================
 */
describe('Module 02 - UoMCategoryForm Component', () => {
  const mockUoMs = [
    { id: 10, code: 'KG', name: 'Kilogram', categoryId: 1 },
    { id: 11, code: 'G', name: 'Gram', categoryId: 1 },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (uomCategoryApi.getAllList as any).mockResolvedValue([]);
    (uomApi.getAllList as any).mockResolvedValue(mockUoMs);
  });

  // #region TC01: RENDER FORM TẠO MỚI
  /**
   * TC01: Form tạo mới phải hiển thị tiêu đề "Thêm Nhóm Đơn Vị Tính" và các trường nhập liệu.
   */
  it('TC01 - Render đầy đủ các trường trong form tạo mới nhóm ĐVT', async () => {
    render(
      <MemoryRouter initialEntries={['/uom-categories/create']}>
        <Routes>
          <Route path="/uom-categories/create" element={<UoMCategoryForm />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Thêm Nhóm Đơn Vị Tính')).toBeInTheDocument();
    expect(screen.getByText('Thông Tin Nhóm')).toBeInTheDocument();
    expect(screen.getByText('Cấu Hình Hệ Thống')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('VD: WEIGHT, LENGTH')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('VD: Khối lượng, Chiều dài')).toBeInTheDocument();
  });
  // #endregion

  // #region TC02: VALIDATION TRƯỜNG BẮT BUỘC
  /**
   * TC02: Bấm Lưu khi chưa nhập thông tin sẽ hiển thị lỗi đỏ ở các ô Mã và Tên nhóm.
   */
  it('TC02 - Báo lỗi validation khi bỏ trống Mã và Tên nhóm', async () => {
    render(
      <MemoryRouter initialEntries={['/uom-categories/create']}>
        <Routes>
          <Route path="/uom-categories/create" element={<UoMCategoryForm />} />
        </Routes>
      </MemoryRouter>
    );

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Vui lòng nhập mã định danh.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng nhập tên nhóm.')).toBeInTheDocument();
    });

    expect(uomCategoryApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC03: CHỌN BASE UOM TỪ DROPDOWN
  /**
   * TC03: Kiểm tra dropdown Đơn vị gốc nạp đúng danh sách UoM options.
   */
  it('TC03 - Nạp danh sách ĐVT cơ sở vào dropdown Base UoM', async () => {
    render(
      <MemoryRouter initialEntries={['/uom-categories/create']}>
        <Routes>
          <Route path="/uom-categories/create" element={<UoMCategoryForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(uomApi.getAllList).toHaveBeenCalled();
    });

    // Dropdown Base UoM có nhãn "Đơn vị gốc (Base UoM)"
    expect(screen.getByText('Đơn vị gốc (Base UoM)')).toBeInTheDocument();
  });
  // #endregion

  // #region TC04: SUBMIT TẠO MỚI THÀNH CÔNG
  /**
   * TC04: Nhập đầy đủ thông tin hợp lệ và submit tạo mới nhóm ĐVT.
   */
  it('TC04 - Submit tạo mới nhóm ĐVT thành công gọi API', async () => {
    (uomCategoryApi.create as any).mockResolvedValue({ id: 1 });

    render(
      <MemoryRouter initialEntries={['/uom-categories/create']}>
        <Routes>
          <Route path="/uom-categories/create" element={<UoMCategoryForm />} />
        </Routes>
      </MemoryRouter>
    );

    const codeInput = screen.getByPlaceholderText('VD: WEIGHT, LENGTH');
    const nameInput = screen.getByPlaceholderText('VD: Khối lượng, Chiều dài');

    fireEvent.change(codeInput, { target: { value: 'VOLUME' } });
    fireEvent.change(nameInput, { target: { value: 'Thể tích' } });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(uomCategoryApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          code: 'VOLUME',
          name: 'Thể tích',
          isActive: true,
          baseUoMId: null,
        })
      );
    });
  });
  // #endregion

  // #region TC05: CHẾ ĐỘ CHỈNH SỬA (EDIT MODE)
  /**
   * TC05: Mở form với param id = 1 -> Gọi API getById và nạp thông tin cũ vào các ô input.
   */
  it('TC05 - Chế độ Edit: Nạp dữ liệu nhóm ĐVT cũ từ API', async () => {
    (uomCategoryApi.getById as any).mockResolvedValue({
      id: 1,
      code: 'WEIGHT',
      name: 'Khối lượng',
      isActive: true,
      baseUoMId: 10,
    });

    render(
      <MemoryRouter initialEntries={['/uom-categories/edit/1']}>
        <Routes>
          <Route path="/uom-categories/edit/:id" element={<UoMCategoryForm />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Chỉnh Sửa Nhóm Đơn Vị')).toBeInTheDocument();

    await waitFor(() => {
      const codeInput = screen.getByPlaceholderText('VD: WEIGHT, LENGTH') as HTMLInputElement;
      const nameInput = screen.getByPlaceholderText(
        'VD: Khối lượng, Chiều dài'
      ) as HTMLInputElement;

      expect(codeInput.value).toBe('WEIGHT');
      expect(nameInput.value).toBe('Khối lượng');
    });
  });
  // #endregion
});
