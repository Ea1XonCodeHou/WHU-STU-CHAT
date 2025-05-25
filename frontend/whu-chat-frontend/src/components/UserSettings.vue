<template>
  <div class="modal-backdrop" @click.self="closeModal">
    <div class="modal-content">
      <div class="modal-header">
        <h3>设置</h3>
        <button class="close-button" @click="closeModal">
          <i class="fas fa-times"></i>
        </button>
      </div>
      
      <div class="modal-body">
        <div class="settings-section">
          <h4 class="section-title">隐私设置</h4>
          
          <div class="setting-item">
            <span class="setting-label">显示在线状态</span>
            <label class="toggle-switch">
              <input type="checkbox" v-model="settings.showMyOnlineStatus">
              <span class="slider"></span>
            </label>
          </div>
          
          <div class="form-actions">
            <button 
              @click="saveSettings" 
              class="save-button" 
              :disabled="isSaving"
            >
              <i v-if="isSaving" class="fas fa-spinner fa-spin"></i>
              <span v-else>保存设置</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, onMounted } from 'vue';
import axios from 'axios';

export default {
  name: 'UserSettings',
  props: {
    visible: {
      type: Boolean,
      required: true
    }
  },
  emits: ['close', 'settings-updated'],
  setup(props, { emit }) {
    const userId = ref(localStorage.getItem('userId') || '');
    
    // 设置项 - 确保默认为true
    const settings = ref({
      showMyOnlineStatus: true // 默认为true
    });
    
    // 保存按钮状态
    const isSaving = ref(false);
    
    // 关闭模态框
    const closeModal = () => {
      // 移除自动加载设置的逻辑，避免意外修改
      emit('close');
    };
    
    // 从服务器加载设置
    const loadSettings = async () => {
      try {
        if (!userId.value) return;
        
        // 首先从localStorage获取设置，如果没有则设置默认值
        const localSetting = localStorage.getItem('setting_showMyOnlineStatus');
        if (localSetting === null) {
          // 如果localStorage中没有设置，设置默认值为true
          localStorage.setItem('setting_showMyOnlineStatus', 'true');
          settings.value.showMyOnlineStatus = true;
        } else {
          // 如果localStorage中有设置，使用该设置
          settings.value.showMyOnlineStatus = localSetting === 'true';
        }
        
        // 尝试从服务器加载设置（可选）
        try {
          const response = await axios.get(`/api/user/${userId.value}/settings`);
          if (response.data && response.data.data) {
            const userSettings = response.data.data;
            if (userSettings['setting_showMyOnlineStatus'] !== undefined) {
              const serverSetting = userSettings['setting_showMyOnlineStatus'] === 'true';
              settings.value.showMyOnlineStatus = serverSetting;
              // 同步到localStorage
              localStorage.setItem('setting_showMyOnlineStatus', serverSetting.toString());
            }
          }
        } catch (serverError) {
          console.log('从服务器加载设置失败，使用本地设置:', serverError);
          // 如果服务器加载失败，保持本地设置不变
        }
      } catch (error) {
        console.error('加载设置失败:', error);
        // 如果加载失败，确保使用默认值
        settings.value.showMyOnlineStatus = true;
        localStorage.setItem('setting_showMyOnlineStatus', 'true');
      }
    };
    
    // 应用设置效果
    const applySettings = async () => {
      localStorage.setItem('setting_showMyOnlineStatus', settings.value.showMyOnlineStatus.toString());
      await updateOnlineStatusVisibility(settings.value.showMyOnlineStatus);
      emit('settings-updated', settings.value);
    };
    
    // 更新用户在线状态可见性
    const updateOnlineStatusVisibility = async (isVisible) => {
      try {
        const userId = localStorage.getItem('userId');
        if (!userId) {
          console.error('用户ID不存在，无法更新在线状态');
          return;
        }
        
        await axios.post(`/api/user/${userId}/status`, {
          userId: userId,
          isOnline: true,
          isVisible: isVisible
        });
        
        console.log('用户在线状态可见性已更新:', isVisible);
      } catch (error) {
        console.error('更新用户在线状态可见性失败:', error);
      }
    };
    
    // 保存设置
    const saveSettings = async () => {
      isSaving.value = true;
      
      try {
        await applySettings();
        
        await axios.post(`/api/user/${userId.value}/settings`, {
          userId: userId.value,
          settingKey: 'setting_showMyOnlineStatus',
          settingValue: settings.value.showMyOnlineStatus.toString()
        });
        
        alert('设置已保存');
      } catch (error) {
        console.error('保存设置失败:', error);
        alert('保存设置失败: ' + error.message);
      } finally {
        isSaving.value = false;
      }
    };
    
    // 组件挂载时
    onMounted(() => {
      if (userId.value) {
        loadSettings();
      }
    });
    
    return {
      settings,
      isSaving,
      closeModal,
      saveSettings
    };
  }
};
</script>

<style scoped>
.modal-backdrop {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}

.modal-content {
  width: 500px;
  max-width: 90%;
  background-color: white;
  border-radius: 8px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
  overflow: hidden;
  max-height: 80vh;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
}

.modal-header h3 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
}

.close-button {
  background: transparent;
  border: none;
  color: white;
  font-size: 16px;
  cursor: pointer;
  padding: 5px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  width: 30px;
  height: 30px;
  transition: background-color 0.2s;
}

.close-button:hover {
  background-color: rgba(255, 255, 255, 0.2);
}

.modal-body {
  padding: 20px;
  overflow-y: auto;
  max-height: calc(80vh - 60px);
}

.settings-section {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.section-title {
  font-size: 16px;
  font-weight: 600;
  color: #4776E6;
  margin-bottom: 10px;
  padding-bottom: 5px;
  border-bottom: 1px solid #eee;
}

.setting-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 0;
}

.setting-label {
  font-size: 14px;
  color: #333;
}

/* 开关样式 */
.toggle-switch {
  position: relative;
  display: inline-block;
  width: 44px;
  height: 22px;
}

.toggle-switch input {
  opacity: 0;
  width: 0;
  height: 0;
}

.slider {
  position: absolute;
  cursor: pointer;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: #ccc;
  transition: .4s;
  border-radius: 22px;
}

.slider:before {
  position: absolute;
  content: "";
  height: 18px;
  width: 18px;
  left: 3px;
  bottom: 2px;
  background-color: white;
  transition: .4s;
  border-radius: 50%;
}

input:checked + .slider {
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
}

input:focus + .slider {
  box-shadow: 0 0 1px #4776E6;
}

input:checked + .slider:before {
  transform: translateX(20px);
}

.save-button {
  width: 100%;
  padding: 10px;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  border: none;
  border-radius: 4px;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.2s;
}

.save-button:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(71, 118, 230, 0.2);
}

.save-button:disabled {
  background: #ccc;
  cursor: not-allowed;
  transform: none;
  box-shadow: none;
}
</style> 