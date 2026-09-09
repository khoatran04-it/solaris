import axiosClient from "./axiosClient";
import { ShopWarehouse } from "@/types/warehouse";

const shopWarehouseApi = {
  getAll: () => axiosClient.get<ShopWarehouse[]>("/warehouses"),
};

export default shopWarehouseApi;
