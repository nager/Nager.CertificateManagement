<template>
  <div>
    <q-btn
      outline
      label="Add Certificate Job"
      class="q-mb-md"
      color="primary"
      @click="dialogVisible = true"
    />
    <q-dialog v-model="dialogVisible">
      <q-layout
        view="Lhh lpR fff"
        container
        class="bg-white"
      >
        <q-header class="bg-primary">
          <q-toolbar>
            <q-toolbar-title>Add Certificate Job</q-toolbar-title>
            <q-btn
              v-close-popup
              flat
              round
              dense
              icon="close"
            />
          </q-toolbar>
        </q-header>
        <q-page-container>
          <q-page padding>
            <q-input
              v-model="fqdn"
              outlined
              class="q-mb-sm"
              label="Fully Qualified Domain Name (e.g. subdomain.mydomain.com)"
            />

            <q-select
              v-model="jobType"
              class="q-mb-md"
              outlined
              :options="options"
              label="Job Type"
            />

            <q-btn
              color="primary"
              label="Add"
              @click="create"
            />
          </q-page>
        </q-page-container>
      </q-layout>
    </q-dialog>
  </div>
</template>

<script>
export default {
  name: 'CertificateJobAdd',
  data () {
    return {
      dialogVisible: false,
      fqdn: '',
      jobType: 'OneTime',
      options: [
        'OneTime', 'AutoRenewal'
      ]
    }
  },
  methods: {
    async create () {
      try {
        await this.$axios.post('/api/CertificateJob', { fqdn: this.fqdn, jobType: 'OneTime' })
        await this.$emit('created')
        this.dialogVisible = false
      } catch (error) {
        this.$q.notify({
          type: 'negative',
          message: 'Request failure',
          caption: `${error}`
        })
      }
    }
  }
}
</script>
