import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import AttributeDefinitionForm from '../../../pages/attributeDefinition/AttributeDefinitionForm';
import { attributeDefinitionApi } from '../../../api/attributeDefinitionApi';

// Mock API
vi.mock('../../../api/attributeDefinitionApi', () => ({
  attributeDefinitionApi: {
    getAllList: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
  },
}));

/**
 * ============================================================================
 * 📦 MODULE 4: ATTRIBUTE DEFINITION
 * 🧪 COMPONENT TEST: AttributeDefinitionForm (Form Thêm / Sửa Từ Điển Thuộc Tính)
 * ============================================================================
 */
describe('Module 04 - AttributeDefinitionForm Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (attributeDefinitionApi.getAllList as any).mockResolvedValue([
      { id: 1, name: 'Độ ngọt (Brix)', dataType: 'NUMBER' },
    ]);
  });

  // #region TC01: RENDER FORM TẠO MỚI
  it('TC01 - Render đầy đủ các trường trong form tạo mới thuộc tính', async () => {
    render(
      <MemoryRouter initialEntries={['/attributes/create']}>
        <Routes>
          <Route path="/attributes/create" element={<AttributeDefinitionForm />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Thêm Thuộc Tính Mới')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('VD: Quy cách đóng gói, Nguồn gốc...')).toBeInTheDocument();
  });
  // #endregion

  // #region TC02: VALIDATION BẮT BUỘC KHI SUBMIT TRỐNG
  it('TC02 - Hiển thị lỗi validation khi submit dữ liệu trống', async () => {
    render(
      <MemoryRouter initialEntries={['/attributes/create']}>
        <Routes>
          <Route path="/attributes/create" element={<AttributeDefinitionForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Thuộc Tính Mới')).toBeInTheDocument();
    });

    const nameInput = screen.getByPlaceholderText('VD: Quy cách đóng gói, Nguồn gốc...');
    fireEvent.change(nameInput, { target: { value: '' } });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Vui lòng nhập tên thuộc tính.')).toBeInTheDocument();
    });

    expect(attributeDefinitionApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC03: CLIENT SIDE DUPLICATE CHECK
  it('TC03 - Báo lỗi trùng lặp tên thuộc tính ngay trên giao diện', async () => {
    render(
      <MemoryRouter initialEntries={['/attributes/create']}>
        <Routes>
          <Route path="/attributes/create" element={<AttributeDefinitionForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Thuộc Tính Mới')).toBeInTheDocument();
    });

    const nameInput = screen.getByPlaceholderText('VD: Quy cách đóng gói, Nguồn gốc...');
    fireEvent.change(nameInput, { target: { value: 'độ ngọt (brix)' } });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Tên thuộc tính này đã tồn tại!')).toBeInTheDocument();
    });

    expect(attributeDefinitionApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC04: SUBMIT FORM TẠO MỚI THÀNH CÔNG
  it('TC04 - Submit form tạo mới thuộc tính thành công và gọi API create', async () => {
    (attributeDefinitionApi.create as any).mockResolvedValue({ id: 2 });

    render(
      <MemoryRouter initialEntries={['/attributes/create']}>
        <Routes>
          <Route path="/attributes/create" element={<AttributeDefinitionForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Thuộc Tính Mới')).toBeInTheDocument();
    });

    const nameInput = screen.getByPlaceholderText('VD: Quy cách đóng gói, Nguồn gốc...');
    fireEvent.change(nameInput, { target: { value: 'Hạn sử dụng' } });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(attributeDefinitionApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          name: 'Hạn sử dụng',
          dataType: 'TEXT',
          isActive: true,
        })
      );
    });
  });
  // #endregion

  // #region TC05: EDIT MODE - LOAD DỮ LIỆU VÀ UPDATE THÀNH CÔNG
  it('TC05 - Load dữ liệu chi tiết ở chế độ Edit và submit cập nhật thành công', async () => {
    (attributeDefinitionApi.getById as any).mockResolvedValue({
      id: 1,
      name: 'Độ ngọt (Brix)',
      dataType: 'NUMBER',
      isActive: true,
    });
    (attributeDefinitionApi.update as any).mockResolvedValue({});

    render(
      <MemoryRouter initialEntries={['/attributes/edit/1']}>
        <Routes>
          <Route path="/attributes/edit/:id" element={<AttributeDefinitionForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Chỉnh Sửa Từ Điển Thuộc Tính')).toBeInTheDocument();
      expect(screen.getByDisplayValue('Độ ngọt (Brix)')).toBeInTheDocument();
    });

    const nameInput = screen.getByDisplayValue('Độ ngọt (Brix)');
    fireEvent.change(nameInput, { target: { value: 'Độ ngọt tiêu chuẩn Brix' } });

    const submitBtn = screen.getByRole('button', { name: /LƯU THAY ĐỔI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(attributeDefinitionApi.update).toHaveBeenCalledWith(
        1,
        expect.objectContaining({
          name: 'Độ ngọt tiêu chuẩn Brix',
        })
      );
    });
  });
  // #endregion
});
