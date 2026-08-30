import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { GhnAddressSelect } from '../../../components/commons/GhnAddressSelect';
import { shippingApi } from '../../../api/shippingApi';

vi.mock('../../../api/shippingApi', () => ({
  shippingApi: {
    getProvinces: vi.fn(),
    getDistricts: vi.fn(),
    getWards: vi.fn(),
  },
}));

describe('Module 14 - GhnAddressSelect Component (Admin)', () => {
  const mockProvinces = [
    { provinceID: 201, provinceName: 'Hồ Chí Minh', code: 'HCM' },
    { provinceID: 202, provinceName: 'Hà Nội', code: 'HN' },
  ];

  const mockDistricts = [
    { districtID: 1442, provinceID: 201, districtName: 'Quận 1', code: 'Q1' },
    { districtID: 1443, provinceID: 201, districtName: 'Quận 3', code: 'Q3' },
  ];

  const mockWards = [
    { wardCode: '20101', districtID: 1442, wardName: 'Phường Bến Nghé' },
    { wardCode: '20102', districtID: 1442, wardName: 'Phường Bến Thành' },
  ];

  const onProvinceChange = vi.fn();
  const onDistrictChange = vi.fn();
  const onWardChange = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    (shippingApi.getProvinces as any).mockResolvedValue(mockProvinces);
    (shippingApi.getDistricts as any).mockResolvedValue(mockDistricts);
    (shippingApi.getWards as any).mockResolvedValue(mockWards);
  });

  it('TC01 - Render 3 dropdowns và tự động load danh sách Tỉnh/Thành phố', async () => {
    render(
      <GhnAddressSelect
        province=""
        district=""
        ward=""
        onProvinceChange={onProvinceChange}
        onDistrictChange={onDistrictChange}
        onWardChange={onWardChange}
      />
    );

    expect(screen.getByText('Tỉnh / Thành Phố')).toBeInTheDocument();
    expect(screen.getByText('Quận / Huyện')).toBeInTheDocument();
    expect(screen.getByText('Phường / Xã')).toBeInTheDocument();

    await waitFor(() => {
      expect(shippingApi.getProvinces).toHaveBeenCalled();
    });
  });

  it('TC02 - Chọn Tỉnh -> Gọi onProvinceChange và kích hoạt tải Quận/Huyện', async () => {
    render(
      <GhnAddressSelect
        province=""
        district=""
        ward=""
        onProvinceChange={onProvinceChange}
        onDistrictChange={onDistrictChange}
        onWardChange={onWardChange}
      />
    );

    const provinceTrigger = screen.getByText('Chọn Tỉnh / Thành...');
    fireEvent.click(provinceTrigger);

    await waitFor(() => {
      expect(screen.getByText('Hồ Chí Minh')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByText('Hồ Chí Minh'));

    expect(onProvinceChange).toHaveBeenCalledWith('Hồ Chí Minh', 201);
  });

  it('TC03 - Chọn Quận/Huyện -> Gọi onDistrictChange và kích hoạt tải Phường/Xã', async () => {
    render(
      <GhnAddressSelect
        province="Hồ Chí Minh"
        district=""
        ward=""
        onProvinceChange={onProvinceChange}
        onDistrictChange={onDistrictChange}
        onWardChange={onWardChange}
      />
    );

    await waitFor(() => {
      expect(shippingApi.getDistricts).toHaveBeenCalledWith(201);
    });

    const districtTrigger = screen.getByText('Chọn Quận / Huyện...');
    fireEvent.click(districtTrigger);

    await waitFor(() => {
      expect(screen.getByText('Quận 1')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByText('Quận 1'));

    expect(onDistrictChange).toHaveBeenCalledWith('Quận 1', 1442);
  });

  it('TC04 - Chọn Phường/Xã -> Gọi onWardChange với tên và mã phường', async () => {
    render(
      <GhnAddressSelect
        province="Hồ Chí Minh"
        district="Quận 1"
        ward=""
        onProvinceChange={onProvinceChange}
        onDistrictChange={onDistrictChange}
        onWardChange={onWardChange}
      />
    );

    await waitFor(() => {
      expect(shippingApi.getWards).toHaveBeenCalledWith(1442);
    });

    const wardTrigger = screen.getByText('Chọn Phường / Xã...');
    fireEvent.click(wardTrigger);

    await waitFor(() => {
      expect(screen.getByText('Phường Bến Nghé')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByText('Phường Bến Nghé'));

    expect(onWardChange).toHaveBeenCalledWith('Phường Bến Nghé', '20101');
  });
});
