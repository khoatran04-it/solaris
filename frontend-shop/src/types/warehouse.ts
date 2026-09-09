export interface ShopWarehouse {
  id: number;
  name: string;
  code: string;
  warehouseType?: string;
  province?: string;
  district?: string;
  ward?: string;
  streetAddress?: string;
  fullAddress?: string;
  latitude?: number;
  longitude?: number;
  isActive: boolean;
}
