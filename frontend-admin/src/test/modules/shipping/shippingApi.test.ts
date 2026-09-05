import { describe, it, expect, vi, beforeEach } from 'vitest';
import axiosClient from '../../../api/axiosClient';
import { shippingApi } from '../../../api/shippingApi';

vi.mock('../../../api/axiosClient', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe('Module 14 - Shipping API Client (Admin)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('TC01 - getProvinces gọi GET /shipping/provinces', async () => {
    const mockProvinces = [
      { provinceID: 201, provinceName: 'Hồ Chí Minh', code: 'HCM' },
      { provinceID: 202, provinceName: 'Hà Nội', code: 'HN' },
    ];
    (axiosClient.get as any).mockResolvedValue(mockProvinces);

    const res = await shippingApi.getProvinces();

    expect(axiosClient.get).toHaveBeenCalledWith('/shipping/provinces');
    expect(res).toEqual(mockProvinces);
  });

  it('TC02 - getDistricts gọi GET /shipping/districts/{provinceId}', async () => {
    const mockDistricts = [
      { districtID: 1442, provinceID: 201, districtName: 'Quận 1', code: 'Q1' },
    ];
    (axiosClient.get as any).mockResolvedValue(mockDistricts);

    const res = await shippingApi.getDistricts(201);

    expect(axiosClient.get).toHaveBeenCalledWith('/shipping/districts/201');
    expect(res).toEqual(mockDistricts);
  });

  it('TC03 - getWards gọi GET /shipping/wards/{districtId}', async () => {
    const mockWards = [{ wardCode: '20101', districtID: 1442, wardName: 'Phường Bến Nghé' }];
    (axiosClient.get as any).mockResolvedValue(mockWards);

    const res = await shippingApi.getWards(1442);

    expect(axiosClient.get).toHaveBeenCalledWith('/shipping/wards/1442');
    expect(res).toEqual(mockWards);
  });

  it('TC04 - calculateFee gọi POST /shipping/calculate-fee', async () => {
    const mockPayload = {
      toDistrictId: 1442,
      toWardCode: '20101',
      subTotal: 350000,
      weightGram: 1000,
    };
    const mockResponse = {
      totalFee: 0,
      originalFee: 28000,
      isFreeShipping: true,
      freeShippingThreshold: 300000,
      amountNeededForFreeShipping: 0,
    };
    (axiosClient.post as any).mockResolvedValue(mockResponse);

    const res = await shippingApi.calculateFee(mockPayload);

    expect(axiosClient.post).toHaveBeenCalledWith('/shipping/calculate-fee', mockPayload);
    expect(res.isFreeShipping).toBe(true);
    expect(res.totalFee).toBe(0);
  });

  it('TC05 - createGhnOrder gọi POST /shipping/ghn/create-order/{orderId}', async () => {
    const mockResponse = {
      orderCode: 'GHN-100830-999',
      expectedDeliveryDate: '2026-09-02',
      totalFee: 25000,
    };
    (axiosClient.post as any).mockResolvedValue(mockResponse);

    const res = await shippingApi.createGhnOrder(100);

    expect(axiosClient.post).toHaveBeenCalledWith('/shipping/ghn/create-order/100');
    expect(res.orderCode).toBe('GHN-100830-999');
  });
});
