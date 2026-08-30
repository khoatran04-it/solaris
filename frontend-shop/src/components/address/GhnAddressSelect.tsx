'use client';

import React, { useEffect, useState } from 'react';
import shopShippingApi from '@/api/shopShippingApi';
import { GhnProvince, GhnDistrict, GhnWard } from '@/types/shipping';

export interface GhnAddressChangePayload {
    province: string;
    district: string;
    ward: string;
    ghnProvinceId?: number;
    ghnDistrictId?: number;
    ghnWardCode?: string;
}

interface GhnAddressSelectProps {
    province: string;
    district: string;
    ward: string;
    onChange: (payload: GhnAddressChangePayload) => void;
    disabled?: boolean;
    required?: boolean;
    className?: string;
}

export default function GhnAddressSelect({
    province,
    district,
    ward,
    onChange,
    disabled = false,
    required = true,
    className = '',
}: GhnAddressSelectProps) {
    const [provinces, setProvinces] = useState<GhnProvince[]>([]);
    const [districts, setDistricts] = useState<GhnDistrict[]>([]);
    const [wards, setWards] = useState<GhnWard[]>([]);

    const [selectedProvinceId, setSelectedProvinceId] = useState<number | null>(null);
    const [selectedDistrictId, setSelectedDistrictId] = useState<number | null>(null);

    // 1. Tải danh sách Tỉnh/Thành phố khi mount
    useEffect(() => {
        shopShippingApi.getProvinces()
            .then((data) => setProvinces(data || []))
            .catch(() => {});
    }, []);

    // 2. Tìm Province ID khi có `province`
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
            shopShippingApi.getDistricts(selectedProvinceId)
                .then((data) => setDistricts(data || []))
                .catch(() => {});
        } else {
            setDistricts([]);
            setWards([]);
        }
    }, [selectedProvinceId]);

    // 4. Tìm District ID khi có `district`
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
            shopShippingApi.getWards(selectedDistrictId)
                .then((data) => setWards(data || []))
                .catch(() => {});
        } else {
            setWards([]);
        }
    }, [selectedDistrictId]);

    const handleProvinceSelect = (e: React.ChangeEvent<HTMLSelectElement>) => {
        const pId = Number(e.target.value);
        const selected = provinces.find((p) => p.provinceID === pId);
        const provinceName = selected?.provinceName || '';

        setSelectedProvinceId(pId || null);
        setSelectedDistrictId(null);
        setDistricts([]);
        setWards([]);

        onChange({
            province: provinceName,
            district: '',
            ward: '',
            ghnProvinceId: pId || undefined,
            ghnDistrictId: undefined,
            ghnWardCode: undefined,
        });
    };

    const handleDistrictSelect = (e: React.ChangeEvent<HTMLSelectElement>) => {
        const dId = Number(e.target.value);
        const selected = districts.find((d) => d.districtID === dId);
        const districtName = selected?.districtName || '';

        setSelectedDistrictId(dId || null);
        setWards([]);

        onChange({
            province,
            district: districtName,
            ward: '',
            ghnProvinceId: selectedProvinceId || undefined,
            ghnDistrictId: dId || undefined,
            ghnWardCode: undefined,
        });
    };

    const handleWardSelect = (e: React.ChangeEvent<HTMLSelectElement>) => {
        const wCode = e.target.value;
        const selected = wards.find((w) => w.wardCode === wCode);
        const wardName = selected?.wardName || '';

        onChange({
            province,
            district,
            ward: wardName,
            ghnProvinceId: selectedProvinceId || undefined,
            ghnDistrictId: selectedDistrictId || undefined,
            ghnWardCode: wCode || undefined,
        });
    };

    return (
        <div className={`grid grid-cols-1 sm:grid-cols-3 gap-4 ${className}`}>
            {/* Tỉnh / Thành phố */}
            <div className="space-y-1">
                <label className="text-xs font-semibold text-slate-700">
                    Tỉnh / Thành phố {required && <span className="text-rose-500">*</span>}
                </label>
                <select
                    value={selectedProvinceId || ''}
                    onChange={handleProvinceSelect}
                    disabled={disabled}
                    required={required}
                    className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-emerald-500 focus:outline-none transition-colors"
                >
                    <option value="">-- Chọn Tỉnh/Thành --</option>
                    {provinces.map((p) => (
                        <option key={p.provinceID} value={p.provinceID}>
                            {p.provinceName}
                        </option>
                    ))}
                </select>
            </div>

            {/* Quận / Huyện */}
            <div className="space-y-1">
                <label className="text-xs font-semibold text-slate-700">
                    Quận / Huyện {required && <span className="text-rose-500">*</span>}
                </label>
                <select
                    value={selectedDistrictId || ''}
                    onChange={handleDistrictSelect}
                    disabled={disabled || !selectedProvinceId}
                    required={required}
                    className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-emerald-500 focus:outline-none transition-colors disabled:opacity-50"
                >
                    <option value="">-- Chọn Quận/Huyện --</option>
                    {districts.map((d) => (
                        <option key={d.districtID} value={d.districtID}>
                            {d.districtName}
                        </option>
                    ))}
                </select>
            </div>

            {/* Phường / Xã */}
            <div className="space-y-1">
                <label className="text-xs font-semibold text-slate-700">
                    Phường / Xã {required && <span className="text-rose-500">*</span>}
                </label>
                <select
                    value={wards.find((w) => w.wardName === ward)?.wardCode || ''}
                    onChange={handleWardSelect}
                    disabled={disabled || !selectedDistrictId}
                    required={required}
                    className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-emerald-500 focus:outline-none transition-colors disabled:opacity-50"
                >
                    <option value="">-- Chọn Phường/Xã --</option>
                    {wards.map((w) => (
                        <option key={w.wardCode} value={w.wardCode}>
                            {w.wardName}
                        </option>
                    ))}
                </select>
            </div>
        </div>
    );
}
