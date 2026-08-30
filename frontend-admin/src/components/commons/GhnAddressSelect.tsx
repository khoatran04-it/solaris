import React, { useEffect, useState } from 'react';
import { FormSelect } from './FormUI';
import { shippingApi } from '../../api/shippingApi';
import { GhnProvince, GhnDistrict, GhnWard } from '../../types/shipping';

export interface GhnAddressSelectProps {
  province: string;
  district: string;
  ward: string;
  onProvinceChange: (provinceName: string, provinceId?: number) => void;
  onDistrictChange: (districtName: string, districtId?: number) => void;
  onWardChange: (wardName: string, wardCode?: string) => void;
  errors?: {
    province?: string;
    district?: string;
    ward?: string;
  };
  disabled?: boolean;
  required?: boolean;
}

export const GhnAddressSelect: React.FC<GhnAddressSelectProps> = ({
  province,
  district,
  ward,
  onProvinceChange,
  onDistrictChange,
  onWardChange,
  errors,
  disabled = false,
  required = true,
}) => {
  const [provinces, setProvinces] = useState<GhnProvince[]>([]);
  const [districts, setDistricts] = useState<GhnDistrict[]>([]);
  const [wards, setWards] = useState<GhnWard[]>([]);

  const [selectedProvinceId, setSelectedProvinceId] = useState<number | null>(null);
  const [selectedDistrictId, setSelectedDistrictId] = useState<number | null>(null);

  // 1. Tải danh sách Tỉnh/Thành phố khi component mount
  useEffect(() => {
    shippingApi
      .getProvinces()
      .then((data) => {
        setProvinces(data || []);
      })
      .catch((err) => {
        console.error('Lỗi tải danh sách tỉnh thành GHN:', err);
      });
  }, []);

  // 2. Tìm Province ID tương ứng khi có giá trị `province` ban đầu (hoặc khi edit mode)
  useEffect(() => {
    if (province && provinces.length > 0) {
      const matched = provinces.find(
        (p) =>
          p.provinceName.toLowerCase() === province.toLowerCase() ||
          p.provinceName.toLowerCase().includes(province.toLowerCase()) ||
          province.toLowerCase().includes(p.provinceName.toLowerCase())
      );
      if (matched) {
        setSelectedProvinceId(matched.provinceID);
      }
    }
  }, [province, provinces]);

  // 3. Tải danh sách Quận/Huyện khi `selectedProvinceId` thay đổi
  useEffect(() => {
    if (selectedProvinceId) {
      shippingApi
        .getDistricts(selectedProvinceId)
        .then((data) => {
          setDistricts(data || []);
        })
        .catch((err) => {
          console.error('Lỗi tải danh sách quận huyện GHN:', err);
        });
    } else {
      setDistricts([]);
      setWards([]);
    }
  }, [selectedProvinceId]);

  // 4. Tìm District ID tương ứng khi có giá trị `district` ban đầu
  useEffect(() => {
    if (district && districts.length > 0) {
      const matched = districts.find(
        (d) =>
          d.districtName.toLowerCase() === district.toLowerCase() ||
          d.districtName.toLowerCase().includes(district.toLowerCase()) ||
          district.toLowerCase().includes(d.districtName.toLowerCase())
      );
      if (matched) {
        setSelectedDistrictId(matched.districtID);
      }
    }
  }, [district, districts]);

  // 5. Tải danh sách Phường/Xã khi `selectedDistrictId` thay đổi
  useEffect(() => {
    if (selectedDistrictId) {
      shippingApi
        .getWards(selectedDistrictId)
        .then((data) => {
          setWards(data || []);
        })
        .catch((err) => {
          console.error('Lỗi tải danh sách phường xã GHN:', err);
        });
    } else {
      setWards([]);
    }
  }, [selectedDistrictId]);

  // Handler khi chọn Tỉnh
  const handleSelectProvince = (provinceName: string) => {
    const selected = provinces.find((p) => p.provinceName === provinceName);
    const pId = selected?.provinceID;
    setSelectedProvinceId(pId || null);
    setSelectedDistrictId(null);
    setDistricts([]);
    setWards([]);

    onProvinceChange(provinceName, pId);
    onDistrictChange('', undefined);
    onWardChange('', undefined);
  };

  // Handler khi chọn Quận/Huyện
  const handleSelectDistrict = (districtName: string) => {
    const selected = districts.find((d) => d.districtName === districtName);
    const dId = selected?.districtID;
    setSelectedDistrictId(dId || null);
    setWards([]);

    onDistrictChange(districtName, dId);
    onWardChange('', undefined);
  };

  // Handler khi chọn Phường/Xã
  const handleSelectWard = (wardName: string) => {
    const selected = wards.find((w) => w.wardName === wardName);
    onWardChange(wardName, selected?.wardCode);
  };

  const provinceOptions = provinces.map((p) => ({
    label: p.provinceName,
    value: p.provinceName,
  }));

  const districtOptions = districts.map((d) => ({
    label: d.districtName,
    value: d.districtName,
  }));

  const wardOptions = wards.map((w) => ({
    label: w.wardName,
    value: w.wardName,
  }));

  return (
    <>
      <FormSelect
        label="Tỉnh / Thành Phố"
        required={required}
        placeholder="Chọn Tỉnh / Thành..."
        showSearch
        searchPlaceholder="Tìm tỉnh thành..."
        value={province}
        error={errors?.province}
        disabled={disabled}
        options={provinceOptions}
        onSelect={handleSelectProvince}
      />

      <FormSelect
        label="Quận / Huyện"
        required={required}
        placeholder={selectedProvinceId ? 'Chọn Quận / Huyện...' : 'Chọn Tỉnh trước'}
        showSearch
        searchPlaceholder="Tìm quận huyện..."
        value={district}
        error={errors?.district}
        disabled={disabled || !selectedProvinceId}
        options={districtOptions}
        onSelect={handleSelectDistrict}
      />

      <FormSelect
        label="Phường / Xã"
        required={required}
        placeholder={selectedDistrictId ? 'Chọn Phường / Xã...' : 'Chọn Quận trước'}
        showSearch
        searchPlaceholder="Tìm phường xã..."
        value={ward}
        error={errors?.ward}
        disabled={disabled || !selectedDistrictId}
        options={wardOptions}
        onSelect={handleSelectWard}
      />
    </>
  );
};
